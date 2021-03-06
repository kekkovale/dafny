// RUN: %dafny /compile:3 "%s" > "%t"
// RUN: %diff "%s.expect" "%t"

method Main()
{
  PrintStream("Up", Up(19));
  PrintStream("Up2", Up2(19));
  PrintStream("UpIf", UpIf(19));
  PrintStream("CUp1", CUp1(19, Blue));
  PrintStream("UpLet0", UpLet0(19));
  PrintStream("UpLet1", UpLet1(19));

  var l := OnlyDs();
  var s := "";
  while |s| < 6
    invariant l == OnlyDs() || l == Nothing()
  {
    print s, if l.nullable then "  yes\n" else "  no\n";
    var ch := if |s| < 3 then 'D' else 'v';
    l := l.deriv(ch);
    s := s + [ch];
  }
  GhostMain();

  var ml := MOnlyDs();
  s := "";
  while |s| < 6
    invariant ml == MOnlyDs() || ml == MNothing()
  {
    print s, if ml.nullable then "  yes\n" else "  no\n";
    var ch := if |s| < 3 then 'D' else 'v';
    ml := if ch in ml.deriv then ml.deriv[ch] else MNothing();
    s := s + [ch];
  }
}

method PrintStream(tag: string, s: Stream<int>)
{
  print tag;
  var n, s := 0, s;
  while n < 5
  {
    print " ", s.head;
    s, n := s.tail, n + 1;
  }
  print "\n";
}

ghost method GhostMain()
{
  var l := IMOnlyDs();
  var s := "";
  while |s| < 6
  {
    var ch := if |s| < 3 then 'D' else 'v';
    l := if ch in l.deriv.Keys then l.deriv[ch] else IML(false, l.deriv);
    s := s + [ch];
  }
}

// ---------------------------------------------------

codatatype Stream<T> = ICons(head: T, tail: Stream<T>)

function method Up(n: int): Stream<int>
{
  ICons(n, Up(n+1))
}

function method Up2(n: int): Stream<int>
{
  ICons(n, ICons(n+1, Up2(n+2)))
}

function method UpIf(n: int): Stream<int>
{
  if n % 2 == 1 then ICons(n, UpIf(n+1)) else ICons(n, UpIf(n+2))
}

function method UpIf'(n: int): Stream<int>
{
  ICons(n, if n % 2 == 1 then UpIf'(n+1) else UpIf'(n+2))
}

datatype Color = Red | Blue

function method CUp0(n: int, c: Color): Stream<int>
{
  match c
  case Red => ICons(n, CUp0(n+1, c))
  case Blue => ICons(n, CUp0(n+2, c))
}

function method CUp1(n: int, c: Color): Stream<int>
{
  ICons(n, match c case Red => CUp1(n+1, c) case Blue => CUp1(n+2, c))
}

function method CUp2(n: int, c: Color): Stream<int>
{
  if c == Red then
    ICons(n, CUp2(n+1, c))
  else
    ICons(n, CUp2(n+2, c))
}

function method CUp3(n: int, c: Color): Stream<int>
{
  ICons(n, if c == Red then CUp3(n+1, c) else CUp3(n+2, c))
}

colemma CUps(n: int, c: Color)
  ensures CUp0(n, c) == CUp1(n, c) == CUp2(n, c) == CUp3(n, c)
{
}

function method UpLet0(n: int): Stream<int>
{
  var n' := n+1;
  ICons(n'-1, UpLet0(n'))
}

function method UpLet1(n: int): Stream<int>
{
  ICons(n, var n' := n+1; UpLet1(n'))
}

// ---------------------------------------------------

codatatype Lang<S> = L(nullable: bool, deriv: S -> Lang<S>)

function method Nothing(): Lang
{
  L(false, s => Nothing())
}

function method OnlyDs(): Lang<char>
{
  L(true, ch => if ch == 'd' || ch == 'D' then OnlyDs() else Nothing())
}

copredicate TotalLang<S>(l: Lang<S>)
  reads *
{
  forall s: S :: l.deriv.reads(s) == {} && l.deriv.requires(s) && TotalLang(l.deriv(s))
}

colemma NothingTotal<S>()
  ensures TotalLang(Nothing<S>())
{
}

colemma OnlyDsTotal()
  ensures TotalLang(OnlyDs())
{
  OnlyDsTotal();
  // surprisingly, NothingTotal<char>() does not need to be invoked in this proof
}

// ---------------------------------------------------

codatatype IMLang<S> = IML(nullable: bool, deriv: imap<S, IMLang<S>>)

function IMNothing(): IMLang
{
  IML(false, imap s {:nowarn} :: IMNothing())
}

function IMOnlyDs(): IMLang<char>
{
  IML(true, imap ch {:nowarn} :: if ch == 'd' || ch == 'D' then IMOnlyDs() else IMNothing())
}

codatatype MLang<S> = ML(nullable: bool, deriv: map<S, MLang<S>>)

function method MNothing(): MLang
{
  ML(false, map s {:nowarn} | s in {} :: MNothing())  // TODO: finiteness check should allow 'false'
}

function method MOnlyDs(): MLang<char>
{
  ML(true, map ch {:nowarn} | ch == 'd' || ch == 'D' :: MOnlyDs())
}
