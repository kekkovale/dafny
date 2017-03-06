module mod1 {

	method Test(a: int) 
		requires 0 < a < 10;
	{
		assert a < 10;
	}

	module nested {
		method nestedTest(a: int)
		{
			assert a > 120;
		}
	}
}


method Test(a: int)
{
	assert a < 10;
}

class class1 {
	method inClassMethod(a: int){
		assert a < 40;
	}
}
