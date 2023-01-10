﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game.Text.SeStringHandling;

namespace GoodMemory;

public class ItemTooltip : IDisposable
{
    public enum TooltipField : byte
    {
        ItemName,
        GlamourName,
        ItemUiCategory,
        ItemDescription = 13,
        Effects = 16,
        SellsFor = 25,
        DurabilityPercent = 28,
        SpiritbondPercent = 30,
        ExtractableProjectableDesynthesizable = 35,
        Param0 = 37,
        Param1 = 38,
        Param2 = 39,
        Param3 = 40,
        Param4 = 41,
        Param5 = 42,
        ShopSellingPrice = 63,
        ControlsDisplay = 64
    }

    private readonly Dictionary<TooltipField, (int size, IntPtr alloc)> tooltipAllocations = new();

    private unsafe byte*** baseTooltipPointer;

    public unsafe SeString this[TooltipField field]
    {
        get => Helper.ReadSeString(*(baseTooltipPointer + 4) + (byte)field);
        set
        {
            var alloc = IntPtr.Zero;
            var size = value.Encode().Length;
            if (tooltipAllocations.ContainsKey(field))
            {
                var (allocatedSize, intPtr) = tooltipAllocations[field];
                if (allocatedSize < size + 128)
                {
                    Marshal.FreeHGlobal(intPtr);
                    tooltipAllocations.Remove(field);
                }
                else
                {
                    alloc = intPtr;
                }
            }

            if (alloc == IntPtr.Zero)
            {
                var allocSize = 64;
                while (allocSize < size + 128) allocSize *= 2;
                alloc = Marshal.AllocHGlobal(allocSize);
                tooltipAllocations.Add(field, (allocSize, alloc));
            }

            Helper.WriteSeString(*(baseTooltipPointer + 4) + (byte)field, alloc, value);
        }
    }

    public SeString this[byte field]
    {
        get => this[(TooltipField)field];
        set => this[(TooltipField)field] = value;
    }

    public void Dispose()
    {
        foreach (var f in tooltipAllocations) Marshal.FreeHGlobal(f.Value.alloc);

        tooltipAllocations.Clear();
    }

    public unsafe void SetPointer(byte*** ptr)
    {
        baseTooltipPointer = ptr;
    }
}