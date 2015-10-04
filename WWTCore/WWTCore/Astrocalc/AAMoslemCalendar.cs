//
//Module : AAMOSLEMCALENDAR.CPP
//Purpose: Implementation for the algorithms which convert between the Julian and Moslem calendars
//Created: PJN / 04-02-2004
//History: PJN / 12-02-2004 1. Replaced all calls to the macro "INT" with the function CAADate::INT which 
//                          is what they should have been.
//         PJN / 26-01-2007 1. Update to fit in with new layout of CAADate class    
//         PJN / 28-01-2007 1. Minor updates to fit in with new layout of CAADate class                          
//
//Copyright (c) 2004 - 2007 by PJ Naughter (Web: www.naughter.com, Email: pjna@naughter.com)
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


/////////////////////// Includes //////////////////////////////////////////////



/////////////////////// Classes ///////////////////////////////////////////////

public class  CAAMoslemCalendar
{
//Static methods

  //////////////////////////// Implementation ///////////////////////////////////
  
  public static CAACalendarDate MoslemToJulian(int Year, int Month, int Day)
  {
	//What will be the return value
	var JulianDate = new CAACalendarDate();
  
	var N = Day + CAADate.INT(29.5001*(Month - 1) + 0.99);
	var Q = CAADate.INT(Year/30.0);
	var R = Year % 30;
	var A = CAADate.INT((11 *R + 3)/30.0);
	var W = 404 *Q + 354 *R + 208 + A;
	var Q1 = CAADate.INT(W/1461.0);
	var Q2 = W % 1461;
	var G = 621 + 4 *CAADate.INT(7 *Q + Q1);
	var K = CAADate.INT(Q2/365.2422);
	var E = CAADate.INT(365.2422 *K);
	var J = Q2 - E + N - 1;
	var X = G + K;
  
	var XMod4 = X % 4;
	if ((J > 366) && (XMod4 == 0))
	{
	  J -= 366;
	  X++;
	}
	if ((J > 365) && (XMod4 > 0))
	{
	  J -= 365;
	  X++;
	}
  
	JulianDate.Year = X;
	CAADate.DayOfYearToDayAndMonth(J, CAADate.IsLeap(X, false), ref JulianDate.Day, ref JulianDate.Month);
  
	return JulianDate;
  }
  public static CAACalendarDate JulianToMoslem(int Year, int Month, int Day)
  {
	//What will be the return value
	var MoslemDate = new CAACalendarDate();
  
	var W = (Year % 4) == 0 ? 2 : 1;
	var N = CAADate.INT((275 * Month)/9.0) - (W *CAADate.INT((Month + 9)/12.0)) + Day - 30;
	var A = Year - 623;
	var B = CAADate.INT(A / 4.0);
	var C = A % 4;
	var C1 = 365.2501 *C;
	var C2 = CAADate.INT(C1);
	if ((C1 - C2) > 0.5)
	  C2++;
  
	var Ddash = 1461 *B + 170 + C2;
	var Q = CAADate.INT(Ddash / 10631.0);
	var R = Ddash % 10631;
	var J = CAADate.INT(R / 354.0);
	var K = R % 354;
	var O = CAADate.INT((11 *J + 14.0) / 30.0);
	var H = 30 *Q + J + 1;
	var JJ = K - O + N - 1;
  
	if (JJ > 354)
	{
	  var CL = H % 30;
	  var DL = (11 *CL + 3) % 30;
	  if (DL < 19)
	  {
		JJ -= 354;
		H++;
	  }
	  else
	  {
		JJ -= 355;
		H++;
	  }
	  if (JJ == 0)
	  {
		JJ = 355;
		H--;
	  }
	}
  
	var S = CAADate.INT((JJ - 1) / 29.5);
	MoslemDate.Month = 1 + S;
	MoslemDate.Day = CAADate.INT(JJ - 29.5 *S);
	MoslemDate.Year = H;
  
	if (JJ == 355)
	{
	  MoslemDate.Month = 12;
	  MoslemDate.Day = 30;
	}
  
	return MoslemDate;
  }
  public static bool IsLeap(int Year)
  {
	var R = Year % 30;
	return ((11 *R + 3) % 30) > 18;
  }
}
