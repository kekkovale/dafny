
lemma dummyLemma()
ensures false
{
  	forall x:int 
	  ensures x == x
	  {
	  }
	t(); 
}

tactic t()
{
  tactic forall forall x :: x == 5 ==> x >= 5
	{
		assume false;
	}	
}

/*
tactic test(){

  var t := forall x :: x > 5 ==> x >= 5 
  tactic forall {:vars z} post()
  {
    tactic var x1
    assume false;
  }
}
*/
