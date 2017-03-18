method setTest(a: set<int>) 
{
	assert 1 in a;
}

method sequenceTest(s: seq<int>)
	requires 0 in s
{
	assert 0 in s;
	assert 1 in s;
}