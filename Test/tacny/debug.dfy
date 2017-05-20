
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
   tac(d);
}

lemma ltest2(d : Dummy)
 ensures true
{
   //tac(d);
}

tactic tac(b: Element)
{

	//     assert true;
	assert false;
	assert true;
	dummyTac(b);

	
}

tactic dummyTac (c: Element)
{
	tmatch c {
    tvar vs := tactic.input;
    tvar ls := tactic.lemmas;	
    explore(ls, vs);
	}
}
