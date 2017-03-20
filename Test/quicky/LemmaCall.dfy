lemma EvenLemma(a: int)
	requires a % 2 == 0
	ensures a % 2 + 1 == 1
{}

method Test(a: int) 
{
	EvenLemma(a);
}