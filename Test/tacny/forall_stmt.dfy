
lemma dummyLemma(x : int)
ensures x > 5
{
	test(); 
}

tactic t()
{
  tactic forall {:vars a,b,c} forall  x :: x == 5 ==> x >= 5
	{
		assume false;
	}	
}


tactic test(){
  
  tvar p :| post_conds();

  tactic forall {:vars z} p
  {
    assume z > 5;
  }
}

