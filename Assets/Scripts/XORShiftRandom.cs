using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XORShiftRandom
{
    ulong seed;
    ulong state;

    public XORShiftRandom(ulong seed)
    {
        this.seed = seed;
        state = seed;
    }

    public ulong Next()
    {
        ulong x = state;
        x ^= x << 13;
        x ^= x >> 7;
        x ^= x << 17;
        x ^= x << 12;
        x ^= x >> 15;
        state = x;
        return (state * 0x2545F4914F6CDD1Dul);
    }
}
