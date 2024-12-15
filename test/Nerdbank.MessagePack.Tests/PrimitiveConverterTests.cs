// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Drawing;

public partial class PrimitiveConverterTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public void SystemDrawingColor() => this.AssertRoundtrip<Color, Witness>(Color.FromArgb(1, 2, 3, 4));

	[Fact]
	public void SystemDrawingPoint() => this.AssertRoundtrip<Point, Witness>(new Point(1, 1));

	[GenerateShape<Point>]
	[GenerateShape<Color>]
	private partial class Witness;
}
