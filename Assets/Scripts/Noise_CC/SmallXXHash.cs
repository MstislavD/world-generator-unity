
public readonly struct SmallXXHash
{
	const float mf = (1f / 4294967295f);

    const uint primeA = 0b10011110001101110111100110110001;
	const uint primeB = 0b10000101111010111100101001110111;
	const uint primeC = 0b11000010101100101010111000111101;
	const uint primeD = 0b00100111110101001110101100101111;
	const uint primeE = 0b00010110010101100110011110110001;

	public static implicit operator uint(SmallXXHash hash) 
	{
		uint avalanche = hash.accumulator;
		avalanche ^= avalanche >> 15;
		avalanche *= primeB;
		avalanche ^= avalanche >> 13;
		avalanche *= primeC;
		avalanche ^= avalanche >> 16;
		return avalanche;
	}

	public static implicit operator SmallXXHash(uint accumulator)=> new SmallXXHash(accumulator);

	public static SmallXXHash operator +(SmallXXHash h, int v) => h.accumulator + (uint)v;

	public static SmallXXHash Seed(int seed) => (uint)seed + primeE;

	public static SmallXXHash Select(SmallXXHash a, SmallXXHash b, bool c) => c ? b.accumulator : a.accumulator;

	static uint RotateLeft(uint data, int steps)
	{
		return (data << steps) | (data >> 32 - steps);
	}

	public readonly uint accumulator;

	public SmallXXHash(uint accumulator)
    {
		this.accumulator = accumulator;
    }

	public SmallXXHash Eat(int data)
    {
		return RotateLeft(accumulator + (uint)data * primeC, 17) * primeD;
    }

	public SmallXXHash Eat(byte data)
	{
		return RotateLeft(accumulator + data * primeE, 11) * primeA;
	}

	public uint ByteA => this & 255;

	public uint ByteB => (this >> 8) & 255;

	public uint ByteC => (this >> 16) & 255;

	public uint ByteD => this >> 24;

	public float Float01A => ByteA * (1f / 255f);

	public float Float01B => ByteB * (1f / 255f);

	public float Float01C => ByteC * (1f / 255f);

	public float Float01D => ByteD * (1f / 255f);

	public int Integer(int minIncl, int maxExcl)
	{
		int delta = maxExcl - minIncl;
		int result = (int)(this * mf * delta) + minIncl;
		return result;
	}

	public uint GetBits(int count, int shift) => (this >> shift) & (uint)((1 << count) - 1);

	public float GetBitsAsFloat01(int count, int shift) => GetBits(count, shift) * (1f / ((1 << count) - 1));
}
