// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples;

internal partial class StructuralEquality
{
#if NET
    #region StructuralEqualityNET
    void Sample()
    {
        var data1a = new MyData { A = "foo", B = new MyDeeperData { C = 5 } };
        var data1b = new MyData { A = "foo", B = new MyDeeperData { C = 5 } };
        var data2 = new MyData { A = "foo", B = new MyDeeperData { C = 4 } };
        Console.WriteLine($"data1a == data1b? {data1a == data1b}"); // false
        Console.WriteLine($"data1a.Equals(data1b)? {data1a.Equals(data1b)}"); // false
        bool equalByValue = StructuralEqualityComparer.GetDefault<MyData>().Equals(data1a, data1b);
        Console.WriteLine($"data1a equal to data1b by value? {equalByValue}"); // true

        Console.WriteLine($"data1a == data2? {data1a == data2}"); // false
        Console.WriteLine($"data1a.Equals(data2)? {data1a.Equals(data2)}"); // false
        equalByValue = StructuralEqualityComparer.GetDefault<MyData>().Equals(data1a, data2);
        Console.WriteLine($"data1a equal to data2 by value? {equalByValue}"); // false
    }

    [GenerateShape]
    internal partial class MyData
    {
        public string? A { get; set; }
        public MyDeeperData? B { get; set; }
    }

    internal class MyDeeperData
    {
        public int C { get; set; }
    }
    #endregion
#else
    #region StructuralEqualityNETFX
    void Sample()
    {
        var data1a = new MyData { A = "foo", B = new MyDeeperData { C = 5 } };
        var data1b = new MyData { A = "foo", B = new MyDeeperData { C = 5 } };
        var data2 = new MyData { A = "foo", B = new MyDeeperData { C = 4 } };
        Console.WriteLine($"data1a == data1b? {data1a == data1b}"); // false
        Console.WriteLine($"data1a.Equals(data1b)? {data1a.Equals(data1b)}"); // false
        bool equalByValue = StructuralEqualityComparer.GetDefault<MyData>(Witness.ShapeProvider).Equals(data1a, data1b);
        Console.WriteLine($"data1a equal to data1b by value? {equalByValue}"); // true

        Console.WriteLine($"data1a == data2? {data1a == data2}"); // false
        Console.WriteLine($"data1a.Equals(data2)? {data1a.Equals(data2)}"); // false
        equalByValue = StructuralEqualityComparer.GetDefault<MyData>(Witness.ShapeProvider).Equals(data1a, data2);
        Console.WriteLine($"data1a equal to data2 by value? {equalByValue}"); // false
    }

    internal class MyData
    {
        public string? A { get; set; }
        public MyDeeperData? B { get; set; }
    }

    internal class MyDeeperData
    {
        public int C { get; set; }
    }

    [GenerateShapeFor<MyData>]
    partial class Witness;
    #endregion
#endif
}
