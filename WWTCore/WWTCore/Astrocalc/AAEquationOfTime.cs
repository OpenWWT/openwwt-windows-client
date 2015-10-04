using System;
//
//Module : AAEQUATIONOFTIME.CPP
//Purpose: Implementation for the algorithms to calculate the "Equation of Time"
//Created: PJN / 29-12-2003
//History: PJN / 05-07-2005 1. Fix for a bug to ensure that values returned from CAAEquationOfTime::Calculate
//                          does not return discontinuities. Instead it now returns negative values when
//                          required.
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

///////////////////////// Includes ////////////////////////////////////////////


/////////////////////// Classes ///////////////////////////////////////////////

public class  CAAEquationOfTime
{
//Static methods

  ///////////////////////// Implementation //////////////////////////////////////
  
  public static double Calculate(double JD)
  {
	var rho = (JD - 2451545) / 365250;
	var rhosquared = rho *rho;
	var rhocubed = rhosquared *rho;
	var rho4 = rhocubed *rho;
	var rho5 = rho4 *rho;
  
	//Calculate the Suns mean longitude
	var L0 = CAACoordinateTransformation.MapTo0To360Range(280.4664567 + 360007.6982779 *rho + 0.03032028 *rhosquared + rhocubed / 49931 - rho4 / 15300 - rho5 / 2000000);
  
	//Calculate the Suns apparent right ascension
	var SunLong = CAASun.ApparentEclipticLongitude(JD);
	var SunLat = CAASun.ApparentEclipticLatitude(JD);
	var epsilon = CAANutation.TrueObliquityOfEcliptic(JD);
	var Equatorial = CAACoordinateTransformation.Ecliptic2Equatorial(SunLong, SunLat, epsilon);
  
	epsilon = CAACoordinateTransformation.DegreesToRadians(epsilon);
	var E = L0 - 0.0057183 - Equatorial.X *15 + CAACoordinateTransformation.DMSToDegrees(0, 0, CAANutation.NutationInLongitude(JD))*Math.Cos(epsilon);
	if (E > 180)
	  E = -(360 - E);
	E *= 4; //Convert to minutes of time
  
	return E;
  }
}
