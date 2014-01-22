

class OurTuple<Type1, Type2> {
	public Type1 Item1 { get; set; }
	public Type2 Item2 { get; set; }

	public OurTuple(Type1 item1, Type2 item2) {
		Item1 = item1;
		Item2 = item2;
	}
}

class OurTuple<Type1, Type2, Type3> {
	public Type1 Item1 { get; set; }
	public Type2 Item2 { get; set; }
	public Type3 Item3 { get; set; }

	public OurTuple(Type1 item1, Type2 item2, Type3 item3) {
		Item1 = item1;
		Item2 = item2;
		Item3 = item3;
	}
}

class OurTuple<Type1, Type2, Type3, Type4> {
	public Type1 Item1 { get; set; }
	public Type2 Item2 { get; set; }
	public Type3 Item3 { get; set; }
	public Type4 Item4 { get; set; }

	public OurTuple(Type1 item1, Type2 item2, Type3 item3, Type4 item4) {
		Item1 = item1;
		Item2 = item2;
		Item3 = item3;
		Item4 = item4;
	}
}