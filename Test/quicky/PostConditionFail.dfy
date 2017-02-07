method PostCondition(a: int, b: int) returns (c: int)
	requires a > 0 && b > 0
	ensures c == a + b
{
	c := a + b +1;
}