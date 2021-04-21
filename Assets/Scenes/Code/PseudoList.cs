using System;
using UnityEngine;

// Token: 0x0200015D RID: 349
public class PseudoList<T>
{
	// Token: 0x06000A8A RID: 2698 RVA: 0x000191C8 File Offset: 0x000173C8
	public PseudoList(int randomness)
	{
		this.randomness = randomness;
		this.Clear();
	}

	// Token: 0x06000A8B RID: 2699 RVA: 0x000191E4 File Offset: 0x000173E4
	public void Clear()
	{
		this.items = new T[0];
		this.list = new T[0];
		this.savedItems = false;
		this.savedList = false;
	}

	// Token: 0x06000A8C RID: 2700 RVA: 0x00019218 File Offset: 0x00017418
	public T GetNext()
	{
		if (this.list.Length == 0)
		{
			return default(T);
		}
		T result = this.list[this.index];
		this.index++;
		if (this.index >= this.list.Length)
		{
			this.Shuffle();
			this.index = 0;
		}
		return result;
	}

	// Token: 0x06000A8D RID: 2701 RVA: 0x00019280 File Offset: 0x00017480
	public void Add(T item)
	{
		T[] array = new T[this.items.Length + 1];
		for (int i = 0; i < this.items.Length; i++)
		{
			array[i] = this.items[i];
		}
		array[this.items.Length] = item;
		this.items = array;
		this.GenerateList();
		this.savedItems = false;
	}

	// Token: 0x06000A8E RID: 2702 RVA: 0x000192EC File Offset: 0x000174EC
	public void GenerateList()
	{
		this.list = new T[this.items.Length * this.randomness];
		int num = 0;
		for (int i = 0; i < this.items.Length; i++)
		{
			for (int j = 0; j < this.randomness; j++)
			{
				this.list[num] = this.items[i];
				num++;
			}
		}
		this.savedList = false;
	}

	// Token: 0x06000A8F RID: 2703 RVA: 0x00019368 File Offset: 0x00017568
	public void Shuffle()
	{
		int i = this.list.Length;
		if (i == 0)
		{
			return;
		}
		while (i > 0)
		{
			i--;
			int num = (int)Mathf.Floor(UnityEngine.Random.value * (float)(i + 1));
			T t = this.list[i];
			T t2 = this.list[num];
			this.list[i] = t2;
			this.list[num] = t;
		}
		this.index = 0;
		this.savedList = false;
	}

	// Token: 0x06000A90 RID: 2704 RVA: 0x000193E8 File Offset: 0x000175E8
	public void Load(int index, T[] items, T[] list)
	{
		this.index = index;
		this.items = items;
		this.list = list;
		this.savedList = true;
		this.savedItems = true;
	}

	// Token: 0x06000A91 RID: 2705 RVA: 0x00019410 File Offset: 0x00017610
	public void ForceSave()
	{
		this.savedList = false;
		this.savedItems = false;
	}

	// Token: 0x06000A92 RID: 2706 RVA: 0x00019420 File Offset: 0x00017620
	public int GetIndex()
	{
		return this.index;
	}

	// Token: 0x06000A93 RID: 2707 RVA: 0x00019428 File Offset: 0x00017628
	public int GetRandomness()
	{
		return this.randomness;
	}

	// Token: 0x06000A94 RID: 2708 RVA: 0x00019430 File Offset: 0x00017630
	public T[] GetItems()
	{
		return this.items;
	}

	// Token: 0x06000A95 RID: 2709 RVA: 0x00019438 File Offset: 0x00017638
	public T[] GetList()
	{
		return this.list;
	}

	// Token: 0x06000A96 RID: 2710 RVA: 0x00019440 File Offset: 0x00017640
	public bool SaveList()
	{
		bool flag = this.savedList;
		this.savedList = true;
		return !flag;
	}

	// Token: 0x06000A97 RID: 2711 RVA: 0x00019460 File Offset: 0x00017660
	public bool SaveItems()
	{
		bool flag = this.savedItems;
		this.savedItems = true;
		return !flag;
	}

	// Token: 0x06000A98 RID: 2712 RVA: 0x00019480 File Offset: 0x00017680
	public override string ToString()
	{
		string text = string.Concat(new object[]
		{
			"( ",
			this.index,
			"-",
			this.randomness,
			" "
		});
		for (int i = this.index; i < this.list.Length; i++)
		{
			text = text + this.list[i] + ",";
		}
		for (int j = 0; j < this.index; j++)
		{
			text = text + this.list[j] + ",";
		}
		return text + " )";
	}

	// Token: 0x0400042D RID: 1069
	private T[] items;

	// Token: 0x0400042E RID: 1070
	private T[] list;

	// Token: 0x0400042F RID: 1071
	private int randomness = 1;

	// Token: 0x04000430 RID: 1072
	private int index;

	// Token: 0x04000431 RID: 1073
	private bool savedItems;

	// Token: 0x04000432 RID: 1074
	private bool savedList;
}
