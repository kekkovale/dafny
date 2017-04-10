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

method validSet(a: set<int>)
{}

method validSequence(a: seq<int>)
{}

method valudArr(a: array<int>)
{}



method rArr(a: array<real>){}
method rSet(a: set<real>){}
method rSeq(a: seq<real>){}

method cArr(a: array<char>){}
method cSet(a: set<char>){}
method cSeq(a: seq<char>){}

method bArr(a: array<bool>){}
method bSet(a: set<bool>){}
method bSeq(a: seq<bool>){}

method nArr(a: array<nat>){}
method nSet(a: set<nat>){}
method nSeq(a: seq<nat>){}



