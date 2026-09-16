using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace checklistWs.Services.Tenant
{
    public sealed record SchemaContract(
        string Scope,
        int ContractVersion,
        string Product,
        IReadOnlyCollection<SchemaTableContract> Tables,
        IReadOnlyCollection<string> Sources,
        DateTime PublishedAtUtc);

    public sealed record SchemaTableContract(
        string Schema,
        string Name,
        IReadOnlyCollection<SchemaColumnContract> Columns,
        SchemaPrimaryKeyContract? PrimaryKey,
        IReadOnlyCollection<SchemaForeignKeyContract> ForeignKeys,
        IReadOnlyCollection<SchemaUniqueContract> UniqueConstraints,
        IReadOnlyCollection<SchemaCheckContract> CheckConstraints,
        IReadOnlyCollection<SchemaIndexContract> Indexes)
    {
        public string FullName => $"{Schema}.{Name}";
    }

    public sealed record SchemaColumnContract(
        string Name,
        string SqlType,
        int? MaxLength,
        byte? Precision,
        int? Scale,
        bool IsNullable,
        string? DefaultDefinition,
        bool IsIdentity,
        bool IsComputed,
        string? ComputedDefinition);

    public sealed record SchemaPrimaryKeyContract(
        string Name,
        IReadOnlyCollection<string> Columns,
        bool IsClustered);

    public sealed record SchemaForeignKeyContract(
        string Name,
        IReadOnlyCollection<string> Columns,
        string ReferencedSchema,
        string ReferencedTable,
        IReadOnlyCollection<string> ReferencedColumns);

    public sealed record SchemaUniqueContract(
        string Name,
        IReadOnlyCollection<string> Columns,
        string? FilterDefinition);

    public sealed record SchemaCheckContract(
        string Name,
        string Definition);

    public sealed record SchemaIndexColumnContract(
        string Name,
        bool IsDescending);

    public sealed record SchemaIndexContract(
        string Name,
        bool IsUnique,
        bool IsClustered,
        IReadOnlyCollection<SchemaIndexColumnContract> KeyColumns,
        IReadOnlyCollection<string> IncludedColumns,
        string? FilterDefinition);

    public sealed record SchemaManifest(
        int ContractVersion,
        string Scope,
        string Product,
        string CanonicalJson,
        string ManifestHash);

    public interface ISchemaContractProvider
    {
        SchemaContract GetContract(string scope, int? version = null);
    }

    public interface ISchemaManifestProvider
    {
        SchemaManifest CreateManifest(SchemaContract contract);
    }

    public sealed class SchemaManifestProvider : ISchemaManifestProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false
        };

        public SchemaManifest CreateManifest(SchemaContract contract)
        {
            if (contract == null)
            {
                throw new ArgumentNullException(nameof(contract));
            }

            var canonical = new
            {
                contract.ContractVersion,
                Product = NormalizeIdentifier(contract.Product),
                Scope = NormalizeIdentifier(contract.Scope),
                Sources = contract.Sources
                    .Select(NormalizeSource)
                    .OrderBy(source => source, StringComparer.Ordinal)
                    .ToArray(),
                Tables = contract.Tables
                    .OrderBy(table => table.Schema, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(table => table.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(table => new
                    {
                        Schema = NormalizeIdentifier(table.Schema),
                        Name = NormalizeIdentifier(table.Name),
                        Columns = table.Columns
                            .OrderBy(column => column.Name, StringComparer.OrdinalIgnoreCase)
                            .Select(column => new
                            {
                                Name = NormalizeIdentifier(column.Name),
                                SqlType = NormalizeSqlType(column.SqlType),
                                column.MaxLength,
                                column.Precision,
                                column.Scale,
                                column.IsNullable,
                                DefaultDefinition = NormalizeDefinition(column.DefaultDefinition),
                                column.IsIdentity,
                                column.IsComputed,
                                ComputedDefinition = NormalizeDefinition(column.ComputedDefinition)
                            })
                            .ToArray(),
                        PrimaryKey = table.PrimaryKey == null ? null : new
                        {
                            Name = NormalizeIdentifier(table.PrimaryKey.Name),
                            Columns = table.PrimaryKey.Columns.Select(NormalizeIdentifier).ToArray(),
                            table.PrimaryKey.IsClustered
                        },
                        ForeignKeys = table.ForeignKeys
                            .OrderBy(fk => fk.Name, StringComparer.OrdinalIgnoreCase)
                            .Select(fk => new
                            {
                                Name = NormalizeIdentifier(fk.Name),
                                Columns = fk.Columns.Select(NormalizeIdentifier).ToArray(),
                                ReferencedSchema = NormalizeIdentifier(fk.ReferencedSchema),
                                ReferencedTable = NormalizeIdentifier(fk.ReferencedTable),
                                ReferencedColumns = fk.ReferencedColumns.Select(NormalizeIdentifier).ToArray()
                            })
                            .ToArray(),
                        UniqueConstraints = table.UniqueConstraints
                            .OrderBy(unique => unique.Name, StringComparer.OrdinalIgnoreCase)
                            .Select(unique => new
                            {
                                Name = NormalizeIdentifier(unique.Name),
                                Columns = unique.Columns.Select(NormalizeIdentifier).ToArray(),
                                FilterDefinition = NormalizeDefinition(unique.FilterDefinition)
                            })
                            .ToArray(),
                        CheckConstraints = table.CheckConstraints
                            .OrderBy(check => check.Name, StringComparer.OrdinalIgnoreCase)
                            .Select(check => new
                            {
                                Name = NormalizeIdentifier(check.Name),
                                Definition = NormalizeDefinition(check.Definition)
                            })
                            .ToArray(),
                        Indexes = table.Indexes
                            .OrderBy(index => index.Name, StringComparer.OrdinalIgnoreCase)
                            .Select(index => new
                            {
                                Name = NormalizeIdentifier(index.Name),
                                index.IsUnique,
                                index.IsClustered,
                                KeyColumns = index.KeyColumns.Select(column => new
                                {
                                    Name = NormalizeIdentifier(column.Name),
                                    column.IsDescending
                                }).ToArray(),
                                IncludedColumns = index.IncludedColumns.Select(NormalizeIdentifier).OrderBy(column => column, StringComparer.OrdinalIgnoreCase).ToArray(),
                                FilterDefinition = NormalizeDefinition(index.FilterDefinition)
                            })
                            .ToArray()
                    })
                    .ToArray()
            };

            string json = JsonSerializer.Serialize(canonical, JsonOptions);
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
            return new SchemaManifest(contract.ContractVersion, contract.Scope, contract.Product, json, hash);
        }

        private static string NormalizeIdentifier(string value)
        {
            return Regex.Replace(value.Trim(), @"\s+", " ");
        }

        private static string NormalizeSource(string value)
        {
            return Regex.Replace(value.Trim(), @"\s+", " ");
        }

        private static string NormalizeSqlType(string value)
        {
            return Regex.Replace(value.Trim().ToUpperInvariant(), @"\s+", string.Empty);
        }

        private static string? NormalizeDefinition(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string normalized = value.Trim();
            normalized = Regex.Replace(normalized, @"\s+", " ");
            normalized = normalized.Replace("( ", "(", StringComparison.Ordinal).Replace(" )", ")", StringComparison.Ordinal);
            return normalized;
        }
    }
}
