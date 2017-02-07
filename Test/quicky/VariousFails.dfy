method PostCondition(a: int, b: int) returns (c: int)
	requires a > 0 && b > 0
	ensures c == a + b
{
	c := a + b +1;
}

method LoopGood(a: int) returns (b: int)
	requires a > 0
	ensures b == a*a
{
	b := 0;
	var i: int := 0;
	while(i < a)
		invariant 0 <= i <= a
		invariant b == a * i;
	{
		b := b + a;
		i := i + 1;
	}
} 

method LoopEntryFail(a: int) returns (b: int)
	requires 0 < a
	ensures b == a*a
{
	b := 0;
	var i: int := 0;
	if(a > 50) {i := 1;}
	while(i < a)
		invariant 0 <= i <= a
		invariant b == a * i;
	{
		b := b + a;
		i := i + 1;
	}
} 
method LoopEndFail(a: int) returns (b: int)
	requires 0 < a
	ensures b == a*a
{
	b := 0;
	var i: int := 0;
	while(i < a)
		invariant 0 <= i <= a
		invariant b == a * i;
	{
		b := b + a;
		i := i + 2;
	}
} 

method TestMethodCall()
{
  var a: int;
  a := LoopGood(5); //TODO: some way to test lower but not count as this layer? skip if no returns? check preconditions?
  
}

method TestAssert(a: int, b: int) {
  assert a < b;
}
