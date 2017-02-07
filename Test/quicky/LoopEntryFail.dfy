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