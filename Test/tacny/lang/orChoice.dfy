
lemma ifIFChoice()
 ensures false
{
   tac();
}



tactic tac()
{
  if (*){
  	assert true;
  }else{
	assume false;
  }
}
