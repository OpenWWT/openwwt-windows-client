using System;
//
//Module : AAANGULARSEPARATION.CPP
//Purpose: Implementation for the algorithms which obtain various separation distances between celestial objects
//Created: PJN / 29-12-2003
//History: None
//
//Copyright (c) 2003 - 2007 by PJ Naughter (Web: www.naughter.com, Email: pjna@naughter.com)
//
//All rights reserved.
//
//Copyright / Usage Details:
//
//You are allowed to include the source code in any product (commercial, shareware, freeware or otherwise) 
//when your product is released in binary form. You are allowed to modify the source code in any way you want 
//except you cannot modify the copyright details at the top of each module. If you want to distribute source 
//code with your application, then you are only allowed to distribute versions released by the author. This is 
//to maintain a single distribution point for the source code. 
//
//
// Converted to c# and distributed with WWT by permision of PJ Naughter by Jonathan Fay
// Please refer to http://www.naughter.com/aa.html for orginal C++ versions
//

//////////////////////////// Includes /////////////////////////////////////////



////////////////////// Classes ////////////////////////////////////////////////

public class CAAAngularSeparation
{
    //Static methods

    //////////////////////////// Implementation ///////////////////////////////////

    public static double Separation(double Alpha1, double Delta1, double Alpha2, double Delta2)
    {
        Delta1 = CAACoordinateTransformation.DegreesToRadians(Delta1);
        Delta2 = CAACoordinateTransformation.DegreesToRadians(Delta2);

        Alpha1 = CAACoordinateTransformation.HoursToRadians(Alpha1);
        Alpha2 = CAACoordinateTransformation.HoursToRadians(Alpha2);

        var x = Math.Cos(Delta1) * Math.Sin(Delta2) - Math.Sin(Delta1) * Math.Cos(Delta2) * Math.Cos(Alpha2 - Alpha1);
        var y = Math.Cos(Delta2) * Math.Sin(Alpha2 - Alpha1);
        var z = Math.Sin(Delta1) * Math.Sin(Delta2) + Math.Cos(Delta1) * Math.Cos(Delta2) * Math.Cos(Alpha2 - Alpha1);

        var @value = Math.Atan2(Math.Sqrt(x * x + y * y), z);
        @value = CAACoordinateTransformation.RadiansToDegrees(@value);
        if (@value < 0)
            @value += 180;

        return @value;
    }
    public static double PositionAngle(double alpha1, double delta1, double alpha2, double delta2)
    {
        var Delta1 = CAACoordinateTransformation.DegreesToRadians(delta1);
        var Delta2 = CAACoordinateTransformation.DegreesToRadians(delta2);

        var Alpha1 = CAACoordinateTransformation.HoursToRadians(alpha1);
        var Alpha2 = CAACoordinateTransformation.HoursToRadians(alpha2);

        var DeltaAlpha = Alpha1 - Alpha2;
        var demoninator = Math.Cos(Delta2) * Math.Tan(Delta1) - Math.Sin(Delta2) * Math.Cos(DeltaAlpha);
        var numerator = Math.Sin(DeltaAlpha);
        var @value = Math.Atan2(numerator, demoninator);
        @value = CAACoordinateTransformation.RadiansToDegrees(@value);

        return @value;
    }
    public static double DistanceFromGreatArc(double Alpha1, double Delta1, double Alpha2, double Delta2, double Alpha3, double Delta3)
    {
        Delta1 = CAACoordinateTransformation.DegreesToRadians(Delta1);
        Delta2 = CAACoordinateTransformation.DegreesToRadians(Delta2);
        Delta3 = CAACoordinateTransformation.DegreesToRadians(Delta3);

        Alpha1 = CAACoordinateTransformation.HoursToRadians(Alpha1);
        Alpha2 = CAACoordinateTransformation.HoursToRadians(Alpha2);
        Alpha3 = CAACoordinateTransformation.HoursToRadians(Alpha3);

        var X1 = Math.Cos(Delta1) * Math.Cos(Alpha1);
        var X2 = Math.Cos(Delta2) * Math.Cos(Alpha2);

        var Y1 = Math.Cos(Delta1) * Math.Sin(Alpha1);
        var Y2 = Math.Cos(Delta2) * Math.Sin(Alpha2);

        var Z1 = Math.Sin(Delta1);
        var Z2 = Math.Sin(Delta2);

        var A = Y1 * Z2 - Z1 * Y2;
        var B = Z1 * X2 - X1 * Z2;
        var C = X1 * Y2 - Y1 * X2;

        var m = Math.Tan(Alpha3);
        var n = Math.Tan(Delta3) / Math.Cos(Alpha3);

        var @value = Math.Asin((A + B * m + C * n) / (Math.Sqrt(A * A + B * B + C * C) * Math.Sqrt(1 + m * m + n * n)));
        @value = CAACoordinateTransformation.RadiansToDegrees(@value);
        if (@value < 0)
            @value = Math.Abs(@value);

        return @value;
    }
    public static double SmallestCircle(double Alpha1, double Delta1, double Alpha2, double Delta2, double Alpha3, double Delta3, ref bool bType1)
    {
        var d1 = Separation(Alpha1, Delta1, Alpha2, Delta2);
        var d2 = Separation(Alpha1, Delta1, Alpha3, Delta3);
        var d3 = Separation(Alpha2, Delta2, Alpha3, Delta3);

        var a = d1;
        var b = d2;
        var c = d3;
        if (b > a)
        {
            a = d2;
            b = d1;
            c = d3;
        }
        if (c > a)
        {
            a = d3;
            b = d1;
            c = d2;
        }

        double @value;
        if (a > Math.Sqrt(b * b + c * c))
        {
            bType1 = true;
            @value = a;
        }
        else
        {
            bType1 = false;
            @value = 2 * a * b * c / (Math.Sqrt((a + b + c) * (a + b - c) * (b + c - a) * (a + c - b)));
        }

        return @value;
    }
}
