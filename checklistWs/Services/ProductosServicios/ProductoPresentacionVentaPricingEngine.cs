using checklistWs.Models.ProductosServicios;

namespace checklistWs.Services.ProductosServicios
{
    // Uses scaled integers so decimal base units never rely on floating-point equality.
    public sealed class ProductoPresentacionVentaPricingEngine
    {
        private const int MaxScaledQuantity = 100000;

        public ProductoServicioPresentacionesVentaCalculoDto Calcular(
            decimal cantidadSolicitada,
            bool permiteDecimales,
            IReadOnlyCollection<ProductoServicioPresentacionVentaDto> presentaciones)
        {
            ProductoServicioPresentacionesVentaCalculoDto result = new()
            {
                CantidadSolicitadaUnidadBase = cantidadSolicitada
            };

            if (cantidadSolicitada <= 0)
            {
                result.Mensaje = "La cantidad solicitada debe ser mayor que cero.";
                return result;
            }

            List<ProductoServicioPresentacionVentaDto> activas = presentaciones
                .Where(item => item.Activo)
                .OrderByDescending(item => item.EquivalenciaBase)
                .ThenBy(item => item.Orden)
                .ThenBy(item => item.Id)
                .ToList();
            if (activas.Count == 0)
            {
                result.Mensaje = "SIN COMBINACION EXACTA";
                return result;
            }

            int scale = permiteDecimales ? GetRequiredScale(cantidadSolicitada, activas) : 1;
            if (!permiteDecimales && decimal.Truncate(cantidadSolicitada) != cantidadSolicitada)
            {
                result.Mensaje = "SIN COMBINACION EXACTA";
                return result;
            }

            int requested;
            try
            {
                requested = checked((int)(cantidadSolicitada * scale));
            }
            catch (OverflowException)
            {
                result.Mensaje = "La cantidad excede el limite seguro de calculo.";
                return result;
            }

            if (requested > MaxScaledQuantity || activas.Any(item => item.EquivalenciaBase * scale != decimal.Truncate(item.EquivalenciaBase * scale)))
            {
                result.Mensaje = "La cantidad o equivalencia excede el limite seguro de calculo.";
                return result;
            }

            int[] equivalencias = activas.Select(item => (int)(item.EquivalenciaBase * scale)).ToArray();
            decimal?[] totals = new decimal?[requested + 1];
            int[] counts = new int[requested + 1];
            int[] previous = Enumerable.Repeat(-1, requested + 1).ToArray();
            int[] chosen = Enumerable.Repeat(-1, requested + 1).ToArray();
            totals[0] = 0m;

            for (int amount = 1; amount <= requested; amount++)
            {
                for (int candidate = 0; candidate < activas.Count; candidate++)
                {
                    int equivalencia = equivalencias[candidate];
                    if (equivalencia > amount || !totals[amount - equivalencia].HasValue)
                    {
                        continue;
                    }

                    decimal total = totals[amount - equivalencia]!.Value + activas[candidate].Precio;
                    int count = counts[amount - equivalencia] + 1;
                    if (!totals[amount].HasValue || total < totals[amount]!.Value ||
                        (total == totals[amount]!.Value && (count < counts[amount] ||
                        (count == counts[amount] && IsPreferred(candidate, amount - equivalencia, chosen[amount], previous[amount], activas, previous, chosen)))))
                    {
                        totals[amount] = total;
                        counts[amount] = count;
                        previous[amount] = amount - equivalencia;
                        chosen[amount] = candidate;
                    }
                }
            }

            if (!totals[requested].HasValue)
            {
                result.Mensaje = "SIN COMBINACION EXACTA";
                return result;
            }

            Dictionary<int, int> selected = new();
            for (int cursor = requested; cursor > 0; cursor = previous[cursor])
            {
                selected[chosen[cursor]] = selected.GetValueOrDefault(chosen[cursor]) + 1;
            }

            result.TieneCombinacionExacta = true;
            result.Mensaje = "Combinacion exacta encontrada.";
            result.UnidadesBaseCubiertas = cantidadSolicitada;
            result.PrecioTotal = totals[requested]!.Value;
            result.Combinacion = selected
                .OrderByDescending(item => activas[item.Key].EquivalenciaBase)
                .ThenBy(item => activas[item.Key].Orden)
                .Select(item => new ProductoServicioPresentacionVentaCalculoLineaDto
                {
                    IdPresentacion = activas[item.Key].Id,
                    Nombre = activas[item.Key].Nombre,
                    EquivalenciaBase = activas[item.Key].EquivalenciaBase,
                    Precio = activas[item.Key].Precio,
                    Cantidad = item.Value,
                    Subtotal = activas[item.Key].Precio * item.Value
                }).ToList();
            return result;
        }

        private static bool IsPreferred(int candidate, int candidatePrevious, int current, int currentPrevious,
            IReadOnlyList<ProductoServicioPresentacionVentaDto> items, int[] previous, int[] chosen)
        {
            if (current < 0) return true;
            List<int> nextPath = Reconstruct(candidatePrevious, previous, chosen);
            nextPath.Add(candidate);
            List<int> currentPath = Reconstruct(currentPrevious, previous, chosen);
            currentPath.Add(current);
            int[] next = nextPath.OrderBy(index => index).ToArray();
            int[] existing = currentPath.OrderBy(index => index).ToArray();
            for (int index = 0; index < next.Length; index++)
            {
                if (next[index] != existing[index]) return next[index] < existing[index];
            }
            return false;
        }

        private static List<int> Reconstruct(int cursor, int[] previous, int[] chosen)
        {
            List<int> path = new();
            for (; cursor > 0; cursor = previous[cursor]) path.Add(chosen[cursor]);
            return path;
        }

        private static int GetRequiredScale(decimal requested, IEnumerable<ProductoServicioPresentacionVentaDto> items)
        {
            int decimals = GetDecimalPlaces(requested);
            foreach (ProductoServicioPresentacionVentaDto item in items)
            {
                decimals = Math.Max(decimals, GetDecimalPlaces(item.EquivalenciaBase));
            }
            return (int)Math.Pow(10, decimals);
        }

        private static int GetDecimalPlaces(decimal value)
        {
            int[] bits = decimal.GetBits(value);
            return (bits[3] >> 16) & 0x7F;
        }
    }
}
