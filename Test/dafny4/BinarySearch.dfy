// RUN: %dafny /compile:0 /dprint:"%t.dprint" "%s" > "%t"
// RUN: %diff "%s.expect" "%t"

// Binary search using standard integers

method BinarySearch(a: array<int>, key: int) returns (r: int)
  requires a != null
  requires forall i,j :: 0 <= i < j < a.Length ==> a[i] <= a[j]
  ensures 0 <= r ==> r < a.Length && a[r] == key
  ensures r < 0 ==> key !in a[..]
{
  var lo, hi := 0, a.Length;
  while lo < hi
    invariant 0 <= lo <= hi <= a.Length
    invariant key !in a[..lo] && key !in a[hi..]
  {
    var mid := (lo + hi) / 2;
    if key < a[mid] {
      hi := mid;
    } else if a[mid] < key {
      lo := mid + 1;
    } else {
      return mid;
    }
  }
  return -1;
}

/*
predicate nuovo(a: array<int>, key: int, r: int)
requires a != null
requires forall i,j :: 0 <= i < j < a.Length ==> a[i] <= a[j]
reads a
{
r < 0 ==> key !in a[..]
}
*/