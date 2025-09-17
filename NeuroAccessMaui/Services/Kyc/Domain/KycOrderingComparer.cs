using System;
using System.Collections.Generic;
using NeuroAccessMaui;
using NeuroAccessMaui.Services.Identity;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Services.Kyc.Domain
{
    /// <summary>
    /// Provides deterministic ordering for mapped properties and summary display items based on the KYC process definition.
    /// </summary>
    internal sealed class KycOrderingComparer : IComparer<string>
    {
        private readonly Dictionary<string, OrderKey> orderByKey;
        private readonly PropertyOrderComparer propertyComparer;
        private readonly DisplayQuadOrderComparer displayComparer;

        private KycOrderingComparer(Dictionary<string, OrderKey> orderByKey)
        {
            this.orderByKey = orderByKey;
            this.propertyComparer = new PropertyOrderComparer(this);
            this.displayComparer = new DisplayQuadOrderComparer(this);
        }

        public static KycOrderingComparer Create(KycProcess process)
        {
            if (process is null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            Dictionary<string, OrderKey> Map = BuildOrderMap(process);
            return new KycOrderingComparer(Map);
        }

        public IComparer<Property> PropertyComparer => this.propertyComparer;

        public IComparer<DisplayQuad> DisplayComparer => this.displayComparer;

        public int Compare(string? left, string? right)
        {
            OrderKey LeftKey = this.GetOrderKey(left);
            OrderKey RightKey = this.GetOrderKey(right);

            int Result = LeftKey.PageIndex.CompareTo(RightKey.PageIndex);
            if (Result != 0)
            {
                return Result;
            }

            Result = LeftKey.FieldIndex.CompareTo(RightKey.FieldIndex);
            if (Result != 0)
            {
                return Result;
            }

            Result = LeftKey.MappingIndex.CompareTo(RightKey.MappingIndex);
            if (Result != 0)
            {
                return Result;
            }

            return string.Compare(LeftKey.Name, RightKey.Name, StringComparison.OrdinalIgnoreCase);
        }

        private OrderKey GetOrderKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return OrderKey.Unknown;
            }

            if (this.orderByKey.TryGetValue(key, out OrderKey Existing))
            {
                return Existing;
            }

            string Normalized = key.Trim();
            return new OrderKey(int.MaxValue, int.MaxValue, int.MaxValue, Normalized);
        }

        private static Dictionary<string, OrderKey> BuildOrderMap(KycProcess process)
        {
            Dictionary<string, OrderKey> Map = new Dictionary<string, OrderKey>(StringComparer.OrdinalIgnoreCase);
            for (int PageIndex = 0; PageIndex < process.Pages.Count; PageIndex++)
            {
                KycPage Page = process.Pages[PageIndex];
                int FieldIndex = 0;

                foreach (ObservableKycField Field in Page.AllFields)
                {
                    RegisterFieldMappings(Map, Field, PageIndex, FieldIndex);
                    FieldIndex++;
                }

                foreach (KycSection Section in Page.AllSections)
                {
                    foreach (ObservableKycField Field in Section.AllFields)
                    {
                        RegisterFieldMappings(Map, Field, PageIndex, FieldIndex);
                        FieldIndex++;
                    }
                }
            }

            return Map;
        }

        private static void RegisterFieldMappings(Dictionary<string, OrderKey> map, ObservableKycField field, int pageIndex, int fieldIndex)
        {
            if (field is null)
            {
                return;
            }

            for (int MappingIndex = 0; MappingIndex < field.Mappings.Count; MappingIndex++)
            {
                KycMapping Mapping = field.Mappings[MappingIndex];
                if (Mapping is null)
                {
                    continue;
                }

                string Key = Mapping.Key ?? string.Empty;
                if (string.IsNullOrWhiteSpace(Key))
                {
                    continue;
                }

                string TrimmedKey = Key.Trim();
                if (map.ContainsKey(TrimmedKey))
                {
                    continue;
                }

                OrderKey Order = new OrderKey(pageIndex, fieldIndex, MappingIndex, TrimmedKey);
                map[TrimmedKey] = Order;

                HandleAliases(map, TrimmedKey, Order);
            }
        }

        private static void HandleAliases(Dictionary<string, OrderKey> map, string key, OrderKey order)
        {
            if (key.Equals(Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) ||
                key.Equals(Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) ||
                key.Equals(Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase))
            {
                RegisterAlias(map, Constants.CustomXmppProperties.BirthDate, order);
            }

            if (key.StartsWith("ORGREP", StringComparison.OrdinalIgnoreCase))
            {
                RegisterAlias(map, "ORGREPBDATE", order);
            }
        }

        private static void RegisterAlias(Dictionary<string, OrderKey> map, string alias, OrderKey order)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                return;
            }

            if (map.ContainsKey(alias))
            {
                return;
            }

            OrderKey AliasOrder = new OrderKey(order.PageIndex, order.FieldIndex, order.MappingIndex, alias.Trim());
            map[alias] = AliasOrder;
        }

        private readonly struct OrderKey
        {
            public OrderKey(int pageIndex, int fieldIndex, int mappingIndex, string name)
            {
                this.PageIndex = pageIndex;
                this.FieldIndex = fieldIndex;
                this.MappingIndex = mappingIndex;
                this.Name = name;
            }

            public int PageIndex { get; }

            public int FieldIndex { get; }

            public int MappingIndex { get; }

            public string Name { get; }

            public static OrderKey Unknown => new OrderKey(int.MaxValue, int.MaxValue, int.MaxValue, string.Empty);
        }

        private sealed class PropertyOrderComparer : IComparer<Property>
        {
            private readonly KycOrderingComparer owner;

            public PropertyOrderComparer(KycOrderingComparer owner)
            {
                this.owner = owner;
            }

            public int Compare(Property? left, Property? right)
            {
                if (left is null && right is null)
                {
                    return 0;
                }

                if (left is null)
                {
                    return 1;
                }

                if (right is null)
                {
                    return -1;
                }

                return this.owner.Compare(left.Name, right.Name);
            }
        }

        private sealed class DisplayQuadOrderComparer : IComparer<DisplayQuad>
        {
            private readonly KycOrderingComparer owner;

            public DisplayQuadOrderComparer(KycOrderingComparer owner)
            {
                this.owner = owner;
            }

            public int Compare(DisplayQuad? left, DisplayQuad? right)
            {
                if (left is null && right is null)
                {
                    return 0;
                }

                if (left is null)
                {
                    return 1;
                }

                if (right is null)
                {
                    return -1;
                }

                int Result = this.owner.Compare(left.Mapping, right.Mapping);
                if (Result != 0)
                {
                    return Result;
                }

                return string.Compare(left.Label, right.Label, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
