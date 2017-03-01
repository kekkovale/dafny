
lemma dummyLemma()
ensures forall x :: x > 5
{
  var z := 0;

	test(); 
}


tactic {:partial} test(){
  
  tvar p :| p in post_conds();

  tactic forall {:vars z} p
  {
    var z0 := forall z1 :: true;
    assume z > 5;
  }
}

