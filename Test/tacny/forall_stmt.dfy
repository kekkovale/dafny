
lemma dummyLemma()
ensures false
{
	forall x : int | x == x
	  ensures x != x
	  {
	    var x := 0;
	    while x < 10{
		  x := x + 1;
		}
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
