# Prepares HO leave: HR review -> Liu Zhiguang (E-IMZO) only (short chain).
param(
  [string]$ApiBase = "http://localhost:5161/api",
  [string]$Password = "12345"
)

$ErrorActionPreference = "Stop"

function Get-Token([string]$Email) {
  $body = @{ email = $Email; password = $Password } | ConvertTo-Json
  try {
    $res = Invoke-RestMethod -Uri "$ApiBase/auth/login" -Method Post -Body $body -ContentType "application/json"
    return $res.accessToken
  } catch {
    throw "Login failed for $Email - set dev password (12345) in Admin or DB."
  }
}

function Invoke-Api {
  param([string]$Token, [string]$Method, [string]$Path, $Body = $null)
  $headers = @{ Authorization = "Bearer $Token" }
  $params = @{ Uri = "$ApiBase$Path"; Method = $Method; Headers = $headers }
  if ($null -ne $Body) {
    $params.Body = ($Body | ConvertTo-Json -Depth 6)
    $params.ContentType = "application/json"
  }
  return Invoke-RestMethod @params
}

Write-Host "==> Creating HO leave request as pangshubao@atg.uz"
$authorToken = Get-Token "pangshubao@atg.uz"
$createBody = @{
  periodLabel = "2026"
  requestDate = (Get-Date).ToString("yyyy-MM-dd")
  items = @(
    @{
      type = "RegularLeave"
      dateFrom = "2026-07-01"
      dateTo = "2026-07-14"
      noteRu = "E-IMZO test"
      noteEn = "E-IMZO test"
    }
  )
}
$created = Invoke-Api -Token $authorToken -Method Post -Path "/hr/leave-requests" -Body $createBody
$id = $created.id
Write-Host "Created $($created.number) ($id)"

Write-Host "==> Submitting"
Invoke-Api -Token $authorToken -Method Post -Path "/hr/leave-requests/$id/submit" | Out-Null

$adminToken = Get-Token "admin@atg.uz"
$userById = @{}
foreach ($u in (Invoke-Api -Token $adminToken -Method Get -Path "/users?page=1&pageSize=500").items) {
  $userById[$u.id] = $u.email
}

for ($i = 1; $i -le 25; $i++) {
  $req = Invoke-Api -Token $adminToken -Method Get -Path "/hr/leave-requests/$id"
  Write-Host "Step $i - phase: $($req.phase)"

  $pending = $req.approvers | Where-Object { $_.status -eq "Pending" } | Select-Object -First 1

  if ($req.phase -eq "AwaitingApproval" -and $pending -and $pending.role -eq "GeneralDirector") {
    Write-Host ""
    Write-Host "=== READY FOR E-IMZO ==="
    Write-Host "Open: http://localhost:3000/ru/hr/leave/$id"
    Write-Host "Login as GD: liuzhiguang@atg.uz / $Password"
    Write-Host "Before signing: Admin -> Users -> liuzhiguang@atg.uz -> set PINPP from your certificate"
    exit 0
  }

  if ($req.phase -eq "Approved") {
    Write-Host "Request approved (no E-IMZO step)."
    exit 0
  }

  $pending = $req.approvers | Where-Object { $_.status -eq "Pending" } | Select-Object -First 1
  if (-not $pending) {
    Write-Host "No pending approver."
    $req.approvers | Format-Table
    exit 1
  }

  $email = $userById[$pending.userId]
  if (-not $email) { throw "Email not found for approver $($pending.userName)" }

  Write-Host "  Acting as $email ($($pending.role))"
  $token = Get-Token $email

  if ($req.phase -eq "HrReview") {
    Invoke-Api -Token $token -Method Post -Path "/hr/leave-requests/$id/hr-review" -Body @{ comment = "HR ok" } | Out-Null
  } elseif ($req.permissions.canApprove) {
    Invoke-Api -Token $token -Method Post -Path "/hr/leave-requests/$id/approve" -Body @{ comment = "OK" } | Out-Null
  } else {
    # Admin view may not have canApprove; approver's own view decides
    $actorReq = Invoke-Api -Token $token -Method Get -Path "/hr/leave-requests/$id"
    if ($actorReq.permissions.canHrReview) {
      Invoke-Api -Token $token -Method Post -Path "/hr/leave-requests/$id/hr-review" -Body @{ comment = "HR ok" } | Out-Null
    } elseif ($actorReq.permissions.canApprove) {
      Invoke-Api -Token $token -Method Post -Path "/hr/leave-requests/$id/approve" -Body @{ comment = "OK" } | Out-Null
    } else {
      throw "User $email cannot act on this request"
    }
  }
}

Write-Host "Exceeded max steps"
exit 1
