using Npgsql;
await using var conn = new NpgsqlConnection("Host=localhost;Port=5432;Database=automation;Username=postgres;Password=postgres");
await conn.OpenAsync();
await using var cmd = new NpgsqlCommand(@"SELECT d.""Number"", d.""Id"", h.""Phase"", h.""OrderNumber"", h.""EimzoCompletedAt"", u.""Email"" as assignee FROM documents d JOIN hr_business_trip_request_details h ON h.""DocumentId"" = d.""Id"" LEFT JOIN users u ON u.""Id"" = d.""AssigneeId"" WHERE d.""Number"" LIKE 'HBO-2026-%' ORDER BY d.""Number""", conn);
await using var r = await cmd.ExecuteReaderAsync();
while (await r.ReadAsync()) Console.WriteLine(string.Join(" | ", r.GetString(0), r.GetGuid(1), r.GetString(2), r.IsDBNull(3)?"":r.GetString(3), r.IsDBNull(4)?"null":r.GetDateTime(4).ToString("o"), r.IsDBNull(5)?"":r.GetString(5)));
