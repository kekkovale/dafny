
datatype Dummy = A | B
datatype Dummy2 = A2 | B2

lemma dummyLemma(m: Dummy)
ensures false
{
	assume false;
}

lemma ltest(d : Dummy)
 ensures false
{
   assert true;assert true;assume false;
}

lemma ltest2(d : Dummy)
 ensures true
{
   assert true;
}

tactic tac(b: Element)
{

	//     assert true;
	assert true;
	assert true;
	dummyTac(b);

	
}

tactic dummyTac (c: Element)
{
	assume false;
}
