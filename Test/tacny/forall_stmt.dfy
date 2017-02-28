
lemma dummyLemma()
ensures forall x :: x > 5
{
  var z := 0;

	test(); 
}


tactic test(){
  
  tvar p :| p in post_conds();

  tactic forall {:vars z} p
  {
    assume z > 5;
  }
}

