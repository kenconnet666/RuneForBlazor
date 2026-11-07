using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rune.Generators;

/// <summary>按内容比较的只读数组：增量生成器的中间结果里放数组时用（普通数组按引用比较，缓存永远不命中）。</summary>
internal readonly struct EquatableArray<T>(T[] items) : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    readonly T[] items = items;

    public bool Equals(EquatableArray<T> other) => AsArray().SequenceEqual(other.AsArray());

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode() => AsArray().Aggregate(17, (hash, item) => hash * 31 + item.GetHashCode());

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)AsArray()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    T[] AsArray() => items ?? [];
}
