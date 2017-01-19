
lemma iftest1()
 ensures false
{
   tacIf();
}

lemma iftest2()
 ensures false
{
   tacElse();
}



tactic tacIf()
{
  if (2 > 1){
  	assume false;
  }
}

tactic tacElse()
{
  if (2 < 1){
  	assert true;
  } else{
    assume false;
  }
}
/*
tactic tacAlt()
{
  if {
  }
}
*/
