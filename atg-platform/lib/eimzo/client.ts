"use client";

import api from "@/lib/api";
import type { PfxCertificate } from "imzo-agnost";
import { CAPIWS, eimzoApi, pfxPlugin, pkcs7Plugin } from "imzo-agnost";
import { getEimzoApiKeyFlat, isEimzoConfigured } from "./config";

export type EimzoCertificate = PfxCertificate;

type CapiwsResponse = {
  success?: boolean;
  reason?: string;
  keyId?: string;
  pkcs7_64?: string;
};

export class EimzoNotConfiguredError extends Error {
  constructor() {
    super("E-IMZO API key is not configured");
    this.name = "EimzoNotConfiguredError";
  }
}

export class EimzoUnavailableError extends Error {
  constructor(message = "E-IMZO desktop application is not available") {
    super(message);
    this.name = "EimzoUnavailableError";
  }
}

let initialized = false;
let resolvedApiKeys: string[] | null = null;

async function resolveApiKeys(): Promise<string[]> {
  if (isEimzoConfigured()) return getEimzoApiKeyFlat();
  if (resolvedApiKeys) return resolvedApiKeys;

  const config = await fetchEimzoConfig();
  resolvedApiKeys = [config.domain, config.apiKey];
  return resolvedApiKeys;
}

export async function ensureEimzoReady() {
  const apiKeys = await resolveApiKeys();
  if (apiKeys.length < 2 || !apiKeys[1]) throw new EimzoNotConfiguredError();

  if (!initialized) {
    await eimzoApi.setupApiKeys(apiKeys);
    await eimzoApi.initialize();
    initialized = true;
  }
}

function callCapiws<T extends CapiwsResponse>(call: {
  plugin: string;
  name: string;
  arguments?: unknown[];
}): Promise<T> {
  return new Promise((resolve, reject) => {
    CAPIWS.callFunction(
      call,
      (_event, data) => {
        if (data?.success) {
          resolve(data as T);
          return;
        }
        reject(new Error(data?.reason || "E-IMZO operation failed"));
      },
      (error) => {
        reject(
          error === 1006 || error === 1000
            ? new EimzoUnavailableError()
            : new Error(typeof error === "string" ? error : "E-IMZO is not available")
        );
      }
    );
  });
}

/** imzo-agnost loadKeyAsync waits for response.data.keyId, but E-IMZO returns keyId at top level. */
async function loadPfxKeyId(certificate: EimzoCertificate): Promise<string> {
  const data = await callCapiws<CapiwsResponse>({
    plugin: "pfx",
    name: "load_key",
    arguments: [certificate.disk || "", certificate.path || "", certificate.name || "", certificate.alias || ""],
  });
  if (!data.keyId) throw new Error("E-IMZO did not return keyId");
  return data.keyId;
}

async function unloadPfxKey(keyId: string) {
  try {
    await callCapiws({ plugin: "pfx", name: "unload_key", arguments: [keyId] });
  } catch {
    // Best-effort cleanup after signing.
  }
}

/** createPkcs7Async has the same response-shape bug; legacy call matches native E-IMZO payload. */
async function createPkcs7Base64(dataBase64: string, keyId: string, detached: "yes" | "no") {
  const data = await pkcs7Plugin.createPkcs7LegacyAsync(dataBase64, keyId, detached);
  const pkcs7 = data.pkcs7_64;
  if (!pkcs7) throw new Error("E-IMZO did not return PKCS#7");
  return pkcs7;
}

export async function listEimzoCertificates(): Promise<EimzoCertificate[]> {
  await ensureEimzoReady();
  const result = await pfxPlugin.listAllCertificatesAsync();
  return result.certificates ?? [];
}

export async function signDetachedBase64(dataBase64: string, certificate: EimzoCertificate) {
  await ensureEimzoReady();
  const keyId = await loadPfxKeyId(certificate);

  try {
    const pkcs7 = await createPkcs7Base64(dataBase64, keyId, "yes");
    const timestamped = await api.post("/eimzo/timestamp", { pkcs7Base64: pkcs7 });
    return timestamped.data.timestampedPkcs7Base64 as string;
  } finally {
    await unloadPfxKey(keyId);
  }
}

export async function signAttachedBase64(dataBase64: string, certificate: EimzoCertificate) {
  await ensureEimzoReady();
  const keyId = await loadPfxKeyId(certificate);

  try {
    const pkcs7 = await createPkcs7Base64(dataBase64, keyId, "no");
    const timestamped = await api.post("/eimzo/timestamp", { pkcs7Base64: pkcs7 });
    return timestamped.data.timestampedPkcs7Base64 as string;
  } finally {
    await unloadPfxKey(keyId);
  }
}

export async function fetchEimzoConfig() {
  const res = await api.get("/eimzo/config");
  return res.data as {
    domain: string;
    apiKey: string;
    timestampProxyUrl: string;
  };
}
