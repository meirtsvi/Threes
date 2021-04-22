using System;
using System.Collections.Generic;
using UnityEngine;

public class NextValueManager
{

	private PseudoList<int> numbers;
	private PseudoList<int> special;
	private PseudoList<int> random;

	private const int kNumberRandomness = 4;
	public const int kSpecialDemotion = 3;
	public const int kSpecialRareness = 20;

	public static int GetValue(int rank)
	{
		return 3 * (int)Mathf.Pow(2f, (float)(rank - 1));
	}

	private int GetNextValue(int numMoves, int highestRank)
	{
		if (numMoves <= 21 || this.special.GetNext() != 1)
		{
			return this.numbers.GetNext();
		}
		int num = Mathf.Max(highestRank - 3);
		if (num < 2)
		{
			return this.numbers.GetNext();
		}
		if (num < 4)
		{
			return GetValue(num);
		}
		return GetValue(UnityEngine.Random.Range(4, num + 1));
	}

	public NextValueManager()
	{
		this.numbers = new PseudoList<int>(kNumberRandomness);
		this.numbers.Add(1);
		this.numbers.Add(2);
		this.numbers.Add(3);
		this.numbers.GenerateList();
		this.numbers.Shuffle();
		this.special = new PseudoList<int>(1);
		this.special.Add(1);
		for (int j = 0; j < kSpecialRareness; j++)
		{
			this.special.Add(0);
		}
		this.special.GenerateList();
		this.special.Shuffle();

	}

	public static int GetRank(int value)
	{
		if (value < 3)
		{
			return 0;
		}
		int num = 1;
		while (value > 3)
		{
			value /= 2;
			num++;
		}
		return num;
	}

	public List<int> PredictFuture(int numMoves, int highestRank)
	{
		List<int> ret = new List<int>();
		int futureValue = GetNextValue(numMoves, highestRank);
		if (futureValue <= 3)
		{
			ret.Add(futureValue);
		}
		else
		{
			int rank = GetRank(futureValue);
			int num = Math.Min(rank - 1, 3);

			for (int i = 0; i < 3; i++)
			{
				if (i < num)
				{
					int calculated_rank = Mathf.Clamp(rank - 1 - i, 1, 11);
					// It took quite some time to figure out that in case of multi value, the tile is not GetValue(rank) but rather GetValue(rank+1)
					ret.Add(GetValue(calculated_rank+1));
				}
			}
		}

		return ret;
	}

}
