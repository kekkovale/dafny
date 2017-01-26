method Test(a: int, b: int) returns (c: int)
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

method LoopEntryBad(a: int) returns (b: int)
	requires 0 < a
	ensures b == a*a
{
	b := 0;
	var i: int := 0;
	if(a > 1000) {i := 1;}
	while(i < a)
		invariant 0 <= i <= a
		invariant b == a * i;
	{
		b := b + a;
		i := i + 1;
	}
} 
method LoopEndBad(a: int) returns (b: int)
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
