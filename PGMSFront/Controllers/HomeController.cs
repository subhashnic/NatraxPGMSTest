using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using PGMSFront.Common;
using PGMSFront.Models;
using PGMSFront.WCFPGMSRef;

namespace PGMSFront.Controllers
{
    public class HomeController : Controller
    {

        #region Global Variables
        Service1Client objServiceClient = new Service1Client();
        ClassUserFunctions objClassUserFunctions = new ClassUserFunctions();
        static string strRptURL = System.Configuration.ConfigurationManager.AppSettings["ReportUrl"];
        static string strPOURL = System.Configuration.ConfigurationManager.AppSettings["strPOURL"];
        #endregion

        #region Login
        public ActionResult Index()
        {
            LoginModel model = new LoginModel();
            try
            {
                Session.Abandon();
                Session.Clear();
            }
            catch
            {
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(LoginModel model)
        {
            try
            {
                ViewData["Resend"] = 0;
                string strMSG = "";
                if (model.LoginId == null || model.LoginId.Trim() == "")
                    strMSG = strMSG + "User Name \n";
                if (model.Password == null || model.Password.Trim() == "")
                    strMSG = strMSG + "Password \n";

                if (strMSG.Trim() == "")
                {
                    string strPSW = DecryptStringAES(model.Password);

                    //Get User By LoginId and Password
                    returndbmlUser objreturndbmlUser = objServiceClient.UserGetByLoginId(model.LoginId, strPSW);

                    //Check User Valid or Not
                    if (objreturndbmlUser.objdbmlStatus != null && objreturndbmlUser.objdbmlStatus.StatusId == 1 && objreturndbmlUser.objdbmlUserView.Count > 0)
                    {

                        dbmlUserView objdbmlUserView = objreturndbmlUser.objdbmlUserView.FirstOrDefault();

                        //Create Session Variable for User 
                        Session["UserId"] = objdbmlUserView.UserId;
                        Session["UserTypePropId"] = objdbmlUserView.UserTypePropId;
                        Session["ZZCompanyId"] = objdbmlUserView.CustomerMasterId;
                        Session["UserName"] = objdbmlUserView.UserName;
                        Session["EmailId"] = objdbmlUserView.EmailId;
                        Session["LoginId"] = objdbmlUserView.LoginId;
                        Session["ZZUserType"] = objdbmlUserView.ZZUserType;
                        Session["UserCode"] = objdbmlUserView.UserCode;
                        Session["StateId"] = objdbmlUserView.ZZStateId;

                        returndbmlProperty objreturndbmlProperty = objServiceClient.PropertiesGetAll();
                        if (objreturndbmlProperty.objdbmlStatus.StatusId == 1 && objreturndbmlProperty.objdbmlProperty.Count > 0)
                        {
                            Session["Properties"] = objreturndbmlProperty.objdbmlProperty;
                        }

                        returndbmlLablinkVorC objreturndbmlLablinkVorC = objServiceClient.LablinkVorCGetAll();
                        if (objreturndbmlLablinkVorC.objdbmlStatus.StatusId == 1 && objreturndbmlLablinkVorC.objdbmlLablinkVorC.Count > 0)
                        {
                            Session["LablinkVorC"] = objreturndbmlLablinkVorC.objdbmlLablinkVorC;
                        }

                        //returndbmlCompanyView objreturndbmlCompanyView = objServiceClient.CompanyViewGetByCompanyId(Convert.ToInt32(objdbmlUserView.CustomerMasterId));
                        //if (objreturndbmlCompanyView.objdbmlStatus.StatusId == 1 && objreturndbmlCompanyView.objdbmlCompanyView.Count > 0)
                        //{
                        //    Session["Company"] = objreturndbmlCompanyView.objdbmlCompanyView.FirstOrDefault();
                        //    Session["StateId"] = objreturndbmlCompanyView.objdbmlCompanyView.FirstOrDefault().StateId;
                        //}

                        return RedirectToAction("Dashboard", "Home");

                    }
                    else
                    {
                        string strErrMSG = objreturndbmlUser.objdbmlStatus.Status;

                        if (objreturndbmlUser.objdbmlStatus.StatusId == 20 || objreturndbmlUser.objdbmlStatus.StatusId == 30)
                        {
                            ViewData["Resend"] = 1;
                            dbmlUserView objdbmlUserView = objreturndbmlUser.objdbmlUserView.FirstOrDefault();
                            Session["dbmlUserView"] = objdbmlUserView;
                            strErrMSG += "\nClick 'OK' to go back to Login Page or 'RESEND' if you have not received the verification link";
                        }

                        //model.Message = strErrMSG;
                        ViewData["MSG"] = strErrMSG;
                    }
                }
                else
                {
                    //model.Message = "Please enter data for Mandatory fields  \n" + strMSG;
                    ViewData["MSG"] = "Please enter data for Mandatory fields  \n" + strMSG;
                }
            }
            catch (Exception ex)
            {
                // model.Message = ex.Message;
                ViewData["MSG"] = ex.Message;
            }

            return View(model);
        }

        [ValidateAntiForgeryToken]
        public ActionResult ResendVerifyLink()
        {
            int intStatusId = 99;
            string strStatus = "Invalid User Details";
            try
            {
                if (Session["dbmlUserView"] != null)
                {
                    dbmlUserView objdbmlUserView = new dbmlUserView();
                    GeneralColl<dbmlUserView>.CopyObject(Session["dbmlUserView"] as dbmlUserView, objdbmlUserView);

                    string strHost = System.Configuration.ConfigurationManager.AppSettings["strHostName"]; //"https://localhost:44307/";
                    string strLink = strHost + "Home/VerifyeMail?xyz=0dfs,ktgbdas,hdffg.khdfrhdduihdgtymdmpxjidgndlxcmhdgmdpldjn,dlkchgj,d,.fddfyre,hjlhhjhjlhjljhjlhdkjdhhdk,dmdhhnd,dkmdndhnndmdkkfbhjyhnhhfssdfgngfgfghgfjfgjgffbgfhfhfhdffdsfdgfdfhfhgfhgfjfwrtwfghkyredcbnmkiufssfgyhvgdrfrthhhjhmjmd&abc="
                                        + objdbmlUserView.UserId
                                        + "&lmn=0dshffn56tgrehbncv6nwyuwgkliurscvjl'ljugbmkl;lkitgn;''lkjhhhjl;llkyhfcfbmkkdfhdfgffhf561g4d5bvgdf1bbdfbdvfgbnvbncvncvbbnxcdgfcbcb";
                    string strFrom = "testdemo052020@gmail.com", strReplyTo = "",
                        strTo = objdbmlUserView.EmailId,
                        strBcc = string.Empty,
                        strCc = string.Empty,
                        strSubject = string.Empty, strBody = string.Empty;


                    strSubject = "Natrax - Verify email";
                    strBody = "Hello, ";

                    strBody += "<br /><br /><b>" + objdbmlUserView.UserName + "</b>";
                    strBody += "<br /><b>" + objdbmlUserView.ZZCompanyName + "</b>";

                    strBody += "<br /><br />You have successfully registered on Natrax with Login-ID <b> '" + objdbmlUserView.LoginId + "</b>'";
                    strBody += ", please click on the below link to verify your email and create passwords";
                    strBody += "<br />" + strLink;



                    strBody += "<br /><br /><br />Regards";
                    strBody += "<br /><br /><span style='font-weight:bold;font-family:Trebuchet MS;font-style:italic'>Natrax Administrator</span>";

                    bool blnSentMail = objClassUserFunctions.SendMailMessage(strFrom, "test@dem0", strTo, strReplyTo, strBcc, strCc, strSubject, strBody, null, "");
                    strStatus = "eMail has been resent successfully";
                    intStatusId = 1;
                }

            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId }, JsonRequestBehavior.AllowGet);
        }

        public static string DecryptStringAES(string cipherText)
        {

            var keybytes = Encoding.UTF8.GetBytes("A51f7e2h2j58r2d5");
            var iv = Encoding.UTF8.GetBytes("A51f7e2h2j58r2d5");

            var encrypted = Convert.FromBase64String(cipherText);
            var decriptedFromJavascript = DecryptStringFromBytes(encrypted, keybytes, iv);
            return string.Format(decriptedFromJavascript);
        }

        private static string DecryptStringFromBytes(byte[] cipherText, byte[] key, byte[] iv)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
            {
                throw new ArgumentNullException("cipherText");
            }
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }
            if (iv == null || iv.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (var rijAlg = new RijndaelManaged())
            {
                //Settings
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.PKCS7;
                rijAlg.FeedbackSize = 128;

                rijAlg.Key = key;
                rijAlg.IV = iv;

                // Create a decrytor to perform the stream transform.
                var decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);
                try
                {
                    // Create the streams used for decryption.
                    using (var msDecrypt = new MemoryStream(cipherText))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {

                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                // Read the decrypted bytes from the decrypting stream
                                // and place them in a string.
                                plaintext = srDecrypt.ReadToEnd();

                            }

                        }
                    }
                }
                catch
                {
                    plaintext = "keyError";
                }
            }

            return plaintext;
        }
        #endregion

        #region Dashboard
        public ActionResult Dashboard()
        {
            CommonModel model = new CommonModel();
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

            }
            catch
            {
            }

            return View(model);
        }

        public ActionResult LoadDashboardInfo()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlDashBoardWorkFlowViewFront objreturndbmlDashBoardWorkFlowViewFront = new returndbmlDashBoardWorkFlowViewFront();
            try
            {
                objreturndbmlDashBoardWorkFlowViewFront = objServiceClient.DashBoardWorkFlowCount(Convert.ToInt32(Session["UserId"]), Convert.ToInt32(Session["ZZCompanyId"]));
                intStatusId = (int)objreturndbmlDashBoardWorkFlowViewFront.objdbmlStatus.StatusId;
                strStatus = objreturndbmlDashBoardWorkFlowViewFront.objdbmlStatus.Status;
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, DashboardList = objreturndbmlDashBoardWorkFlowViewFront.objdbmlDashBoardWorkFlowViewFront }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Booking

        #region Booking Search/New/Inprocess
        public ActionResult ManageBooking()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Track";
                Session["SessHistoryType"] = "ManageBooking";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
                ViewBag.BookingType = GetBookingType();
                ViewBag.BookingStatus = GetBookingStatus();
            }
            catch
            {
            }

            return View(model);
        }

        [ValidateAntiForgeryToken]
        public ActionResult BookingGetByCompanyIdStatusPropId()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlBooking objreturndbmlBooking = new returndbmlBooking();

            try
            {
                int intCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                int intStatusPropId = 40;

                objreturndbmlBooking = objServiceClient.BookingViewGetByCompanyIdStatusPropId(intCompanyId, intStatusPropId);

                if (objreturndbmlBooking != null && objreturndbmlBooking.objdbmlStatus.StatusId == 1)
                {
                    intStatusId = 1;
                    strStatus = "Success";
                }
                else
                {
                    strStatus = objreturndbmlBooking.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, BookingList = objreturndbmlBooking.objdbmlBookingList }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult BookingSearchViewGetByDepartmentBookinStatus(int intDepartmentId, int intBookingTypeId, int intStatusPropId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlBookingSearchView objreturndbmlBookingSearchView = new returndbmlBookingSearchView();

            try
            {
                objreturndbmlBookingSearchView = objServiceClient.BookingSearchViewGetByCompanyIdFromDateToDateFront(intDepartmentId, DateTime.Now.Date, DateTime.Now.Date, intBookingTypeId, intStatusPropId);

                if (objreturndbmlBookingSearchView != null && objreturndbmlBookingSearchView.objdbmlStatus.StatusId == 1)
                {
                    intStatusId = 1;
                    strStatus = "Success";
                }
                else
                {
                    strStatus = objreturndbmlBookingSearchView.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, BookingList = objreturndbmlBookingSearchView.objdbmlBookingSearchView }, JsonRequestBehavior.AllowGet);
        }

        //[ValidateAntiForgeryToken]
        public ActionResult BookingGetByBookingId(int intBookingId)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            returndbmlBooking objreturndbmlBooking = new returndbmlBooking();
            try
            {
                objreturndbmlBooking = objServiceClient.BookingViewGetByBookingId(intBookingId);

                if (objreturndbmlBooking != null && objreturndbmlBooking.objdbmlStatus.StatusId == 1)
                {
                    Session["objdbmlBooking"] = objreturndbmlBooking.objdbmlBookingList.FirstOrDefault();
                    Session["BPId"] = objreturndbmlBooking.objdbmlBookingList.FirstOrDefault().BPId;
                }
            }
            catch
            {

            }

            return RedirectToAction("Basic", "Home");
        }

        public ActionResult NewBooking(int intBPId)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                Session["BPId"] = intBPId;
            }
            catch
            {

            }

            return RedirectToAction("Basic", "Home");
        }

        public List<SelectListItem> GetBookingType()
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                Items.Add(new SelectListItem { Text = "Booking", Value = Convert.ToString(Convert.ToInt32(HardCodeValues.BookingBPId)) });
                Items.Add(new SelectListItem { Text = "RFQ - Confidential", Value = Convert.ToString(Convert.ToInt32(HardCodeValues.RFQConfBPId)) });
                Items.Add(new SelectListItem { Text = "RFQ - Regular", Value = Convert.ToString(Convert.ToInt32(HardCodeValues.RFQRegBPId)) });
            }
            catch
            {

            }
            return Items;
        }

        public List<SelectListItem> GetLabBookingType()
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                Items.Add(new SelectListItem { Text = "Lab Booking", Value = Convert.ToString(Convert.ToInt32(HardCodeValues.LabBookingBPId)) });
                Items.Add(new SelectListItem { Text = "Lab RFQ", Value = Convert.ToString(Convert.ToInt32(HardCodeValues.LabRFQRegBPId)) });
            }
            catch
            {

            }
            return Items;
        }

        public List<SelectListItem> GetBookingStatus()
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                Items.Add(new SelectListItem { Text = "All", Value = "0" });
                Items.Add(new SelectListItem { Text = "In progress", Value = "40" });
                Items.Add(new SelectListItem { Text = "Approve", Value = "38" });
            }
            catch
            {

            }
            return Items;
        }

        //public List<SelectListItem> GetBookingWorkFlowStatus(int intBPId)
        //{
        //    List<SelectListItem> Items = new List<SelectListItem>();
        //    try
        //    {
        //        ObservableCollection<dbmlWorkFlowView> objdbmlWorkFlowView = WorkFlowViewGetByBPId(intBPId, 0);

        //        foreach (var itm in objdbmlWorkFlowView)
        //        {
        //            Items.Add(new SelectListItem { Text = itm.WorkFlowName, Value = itm.WorkFlowId.ToString() });
        //        }
        //    }
        //    catch
        //    {

        //    }
        //    return Items;
        //}

        public ActionResult TrackBookingHistory()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Track";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
                ViewBag.BookingType = GetBookingType();
                //ViewBag.BookingStatus = GetBookingStatus();
            }
            catch
            {
            }

            return View(model);
        }

        public ActionResult LabBookingHistory()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Lab";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
                ViewBag.BookingType = GetLabBookingType();
                //ViewBag.BookingStatus = GetBookingStatus();
            }
            catch
            {
            }

            return View(model);
        }

        [ValidateAntiForgeryToken]
        public ActionResult GetBookingWorkFlowStatusGetByBPId(int intBPId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            ObservableCollection<dbmlWorkFlowView> objdbmlWorkFlowView = WorkFlowViewGetByBPId(intBPId, 0);
            intStatusId = 1;
            strStatus = "Success";

            return Json(new { Status = strStatus, StatusId = intStatusId, WorkFlowViewList = objdbmlWorkFlowView }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Track/Lab RFQ
        public ActionResult TrackBookingsAndRFQ()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Track";
                Session["SessHistoryType"] = "RFQ";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

                //ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
                //ViewBag.BookingType = GetBookingType();
                //ViewBag.BookingStatus = GetBookingStatus();
            }
            catch
            {
            }

            return View(model);
        }

        public ActionResult LabBookingsAndRFQ()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Lab";
                Session["SessHistoryType"] = "RFQ";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);


            }
            catch
            {
            }

            return View(model);
        }

        public ActionResult BookingStandard()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Track";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
            }
            catch
            {
            }

            return View(model);
        }

        public ActionResult BookingAgainstRFQ()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Track";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
            }
            catch
            {
            }

            return View(model);
        }

        public ActionResult BookingAgainstRFQConf()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Track";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
                //ViewBag.BookingType = GetBookingType();
                //ViewBag.BookingStatus = GetBookingStatus();
            }
            catch
            {
            }

            return View(model);
        }

        public ActionResult BookingAgainstContract()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Track";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
                //ViewBag.BookingType = GetBookingType();
                //ViewBag.BookingStatus = GetBookingStatus();
            }
            catch
            {
            }

            return View(model);
        }

        [ValidateAntiForgeryToken]
        public ActionResult RFQBookingSearchViewGetByDepartmentBookinStatus(int intDepartmentId, int intBookingTypeId, int intStatusPropId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlBookingSearchView objreturndbmlBookingSearchView = new returndbmlBookingSearchView();

            try
            {
                objreturndbmlBookingSearchView = objServiceClient.RFQBookingSearchViewFrontGetByCompanyIdFromDateToDate(intDepartmentId, DateTime.Now.Date, DateTime.Now.Date, intBookingTypeId, intStatusPropId);

                if (objreturndbmlBookingSearchView != null && objreturndbmlBookingSearchView.objdbmlStatus.StatusId == 1)
                {
                    intStatusId = 1;
                    strStatus = "Success";
                }
                else
                {
                    strStatus = objreturndbmlBookingSearchView.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, BookingList = objreturndbmlBookingSearchView.objdbmlBookingSearchView }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult RFQBookingDetailInsert(int intRFQBookingId, int intRFQBPId, int intBPId)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            returndbmlBooking objreturndbmlBooking = new returndbmlBooking();
            try
            {
                objreturndbmlBooking = objServiceClient.RFQBookingDetailInsertByBookingIdBPId(intRFQBookingId, intRFQBPId, intBPId, Convert.ToInt32(Session["UserId"]), Convert.ToInt32(Session["ZZCompanyId"]));

                if (objreturndbmlBooking != null && objreturndbmlBooking.objdbmlStatus.StatusId == 1)
                {
                    Session["objdbmlBooking"] = objreturndbmlBooking.objdbmlBookingList.FirstOrDefault();
                    Session["BPId"] = objreturndbmlBooking.objdbmlBookingList.FirstOrDefault().BPId;
                }
                else
                {
                    return Json(new { Status = objreturndbmlBooking.objdbmlStatus.Status.ToString(), StatusId = -99 }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { Status = ex.Message, StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            return RedirectToAction("Basic", "Home");
        }

        public ActionResult BookingRFQ()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Track";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
            }
            catch
            {
            }

            return View(model);
        }

        public ActionResult BookingRFQConf()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Track";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
            }
            catch
            {
            }

            return View(model);
        }

        public ActionResult BookingContract()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Track";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
            }
            catch
            {
            }

            return View(model);
        }

        public ActionResult LabBookingStandard()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Track";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
            }
            catch
            {
            }

            return View(model);
        }

        public ActionResult LabBookingRFQ()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Track";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
            }
            catch
            {
            }

            return View(model);
        }

        public ActionResult LabBookingAgainstRFQ()
        {
            CommonModel model = new CommonModel();
            try
            {
                Session["SessBookingType"] = "Track";

                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                Session["objdbmlBooking"] = null;

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
            }
            catch
            {
            }

            return View(model);
        }

        [ValidateAntiForgeryToken]
        public ActionResult DashBoardDocumentGetByBPIdWorkFlowIdStatusPropertyId(int intBPId, int intWorkFlowId, string strStatusPropId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlDashBoardDocumentViewFront objreturndbmlDashBoardDocumentViewFront = new returndbmlDashBoardDocumentViewFront();

            try
            {
                objreturndbmlDashBoardDocumentViewFront = objServiceClient.DashBoardDocumentGetByBPIdWorkFlowIdStatusPropertyId(intBPId, intWorkFlowId, strStatusPropId);

                if (objreturndbmlDashBoardDocumentViewFront != null && objreturndbmlDashBoardDocumentViewFront.objdbmlStatus.StatusId == 1)
                {
                    intStatusId = 1;
                    strStatus = "Success";
                }
                else
                {
                    strStatus = objreturndbmlDashBoardDocumentViewFront.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, DashBoardDocumentList = objreturndbmlDashBoardDocumentViewFront.objdbmlDashBoardDocumentViewFront }, JsonRequestBehavior.AllowGet);
        }
        
        [ValidateAntiForgeryToken]
        public ActionResult ToDoBookingSearchViewGetByCompanyIdFromDateToDateFront()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlBookingSearchView objreturndbmlBookingSearchView = new returndbmlBookingSearchView();

            try
            {
                objreturndbmlBookingSearchView = objServiceClient.ToDoBookingSearchViewGetByCompanyIdFromDateToDateFront(0, 0,DateTime.Now.Date, DateTime.Now.Date, 0, 0);

                if (objreturndbmlBookingSearchView != null && objreturndbmlBookingSearchView.objdbmlStatus.StatusId == 1)
                {
                    intStatusId = 1;
                    strStatus = "Success";
                }
                else
                {
                    strStatus = objreturndbmlBookingSearchView.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, BookingList = objreturndbmlBookingSearchView.objdbmlBookingSearchView }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Basic
        public ActionResult Basic()
        {
            CommonModel model = new CommonModel();
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);
                model.StateId = Convert.ToInt32(Session["StateId"]);
                model.BPId = Convert.ToInt32(Session["BPId"]);
                model.ReportURL = strRptURL;

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = Convert.ToInt32(objdbmlBooking.ZZWorkFlowId);
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;
                    model.StatusPropId = Convert.ToInt32(objdbmlBooking.StatusPropId);
                    model.DocId = objdbmlBooking.BookingId;
                    model.WorkFlowView = WorkFlowViewGetByBPId(Convert.ToInt32(Session["BPId"]), objdbmlBooking.BookingId);
                    model.POURL = strPOURL + objdbmlBooking.PODocPath;
                    model.RFQId = Convert.ToInt32(objdbmlBooking.RFQId);
                    model.RFQBPId = Convert.ToInt32(objdbmlBooking.ZZRFQBPId);
                    model.RFQBookingNo = objdbmlBooking.ZZRFQBookingNo;
                    ViewBag.WorkflowRemark = objdbmlBooking.ZZWorkflowRemark;
                    switch (Convert.ToInt32(Session["BPId"]))
                    {
                        case 21:
                            model.DocType = "Track Booking";
                            Session["SessBookingType"] = "Track";
                            break;
                        case 98:
                            model.DocType = "Track RFQ - Confidential";
                            Session["SessBookingType"] = "Track";
                            break;
                        case 46:
                            model.DocType = "Track RFQ - Regular";
                            Session["SessBookingType"] = "Track";
                            break;
                        case 90:
                            model.DocType = "Lab Booking";
                            Session["SessBookingType"] = "Lab";
                            break;
                        case 91:
                            model.DocType = "Lab RFQ - Regular";
                            Session["SessBookingType"] = "Lab";
                            break;
                    }
                }
                else
                {
                    model.DocDate = "To be allotted";
                    model.DocNo = "To be allotted";

                    switch (Convert.ToInt32(Session["BPId"]))
                    {
                        case 21:
                            model.WorkFlowId = Convert.ToInt32(HardCodeValues.BookingWFId);
                            model.DocType = "Track Booking";
                            Session["SessBookingType"] = "Track";
                            break;
                        case 98:
                            model.WorkFlowId = Convert.ToInt32(HardCodeValues.RFQConfWFId);
                            model.DocType = "Track RFQ - Confidential";
                            Session["SessBookingType"] = "Track";
                            break;
                        case 46:
                            model.WorkFlowId = Convert.ToInt32(HardCodeValues.RFQRegWFId);
                            model.DocType = "Track RFQ - Regular";
                            Session["SessBookingType"] = "Track";
                            break;
                        case 90:
                            model.WorkFlowId = Convert.ToInt32(HardCodeValues.LabBookingWFId);
                            model.DocType = "Lab Booking";
                            Session["SessBookingType"] = "Lab";
                            break;
                        case 91:
                            model.WorkFlowId = Convert.ToInt32(HardCodeValues.LabRFQRegWFId);
                            model.DocType = "Lab RFQ - Regular";
                            Session["SessBookingType"] = "Lab";
                            break;
                    }

                    model.StatusPropId = Convert.ToInt32(HardCodeValues.OpenStatusId);
                    model.WorkFlowView = WorkFlowViewGetByBPId(Convert.ToInt32(Session["BPId"]), 0);
                    model.RFQId = 0;
                    model.RFQBPId = 0;
                    model.RFQBookingNo = "";
                }
            }
            catch
            {
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Basic(CommonModel model, string btnPrevNext)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                if (btnPrevNext.ToLower() == "next")
                {
                    return RedirectToAction("Vehicle", "Home");
                }
            }
            catch
            {
            }

            return RedirectToAction("Basic", "Home");
        }

        [ValidateAntiForgeryToken]
        public ActionResult LoadBasicInfo()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            ObservableCollection<dbmlBookingView> objdbmlBookingList = new ObservableCollection<dbmlBookingView>();
            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    objdbmlBookingList.Add(objdbmlBooking);

                    intStatusId = 1;
                    strStatus = "Success";
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, BookingList = objdbmlBookingList }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult BookingSave(dbmlBookingView model)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlBooking objreturndbmlBooking = new returndbmlBooking();

            try
            {
                model.BookingDate = DateTime.Now.Date;//objClassUserFunctions.ToDateTimeNotNull(model.ZZBookingDate);
                model.BPId = Convert.ToInt32(Session["BPId"]);
                model.CompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.CreateId = Convert.ToInt32(Session["UserId"]);
                model.CreateDate = DateTime.Now;
                model.UpdateId = Convert.ToInt32(Session["UserId"]);
                model.UpdateDate = DateTime.Now;
                model.StatusPropId = Convert.ToInt32(HardCodeValues.OpenStatusId);
                if (model.BookingId <= 0)
                {
                    model.BookingNo = "Temp";
                }

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.TabStatusId = objdbmlBooking.TabStatusId;
                }
                else
                {
                    model.TabStatusId = 0;
                }


                returndbmlBooking objreturndbmlBookingTemp = new returndbmlBooking();
                ObservableCollection<dbmlBookingView> objdbmlBookingList = new ObservableCollection<dbmlBookingView>();
                objdbmlBookingList.Add(model);
                objreturndbmlBookingTemp.objdbmlBookingList = objdbmlBookingList;

                if (model.BookingId <= 0)
                {
                    objreturndbmlBooking = objServiceClient.BookingInsert(objreturndbmlBookingTemp);
                }
                else
                {
                    objreturndbmlBooking = objServiceClient.BookingUpdate(objreturndbmlBookingTemp);
                }

                if (objreturndbmlBooking != null && objreturndbmlBooking.objdbmlStatus.StatusId == 1)
                {
                    Session["objdbmlBooking"] = objreturndbmlBooking.objdbmlBookingList.FirstOrDefault();
                    intStatusId = 1;
                    strStatus = "Data Saved Successfully";
                }
                else
                {
                    strStatus = objreturndbmlBooking.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, BookingList = objreturndbmlBooking.objdbmlBookingList }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult BookingDelete(int intBookingId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlStatus objreturndbmlStatus = new returndbmlStatus();

            try
            {
                objreturndbmlStatus = objServiceClient.BookingDeleteAllByBookingId(intBookingId);

                if (objreturndbmlStatus != null && objreturndbmlStatus.objdbmlStatus.StatusId == 1)
                {
                    Session["objdbmlBooking"] = null;
                    intStatusId = 1;
                    strStatus = "Success";
                }
                else
                {
                    strStatus = objreturndbmlStatus.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId }, JsonRequestBehavior.AllowGet);
        }

        public List<SelectListItem> CompanyDepartmentGetByCustomerMasterId(int intCustomerMasterId)
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                returndbmlCompanyDepartment objreturndbmlCompanyDepartment = objServiceClient.CompanyDepartmentGetByCustomerMasterId(intCustomerMasterId);
                if (objreturndbmlCompanyDepartment != null && objreturndbmlCompanyDepartment.objdbmlStatus.StatusId == 1)
                {
                    foreach (var itm in objreturndbmlCompanyDepartment.objdbmlCompanyDepartment)
                    {
                        Items.Add(new SelectListItem { Text = itm.Department, Value = itm.CompanyDepartmentId.ToString(), Selected = false });
                    }
                }
            }
            catch
            {

            }
            return Items;
        }

        public ObservableCollection<dbmlWorkFlowView> WorkFlowViewGetByBPId(int intBPId, int intDocId)
        {
            ObservableCollection<dbmlWorkFlowView> objdbmlWorkFlowView = new ObservableCollection<dbmlWorkFlowView>();
            try
            {
                //if (Session["WorkFlowView"] != null)
                //{
                //    GeneralColl<dbmlWorkFlowView>.CopyCollection(Session["WorkFlowView"] as ObservableCollection<dbmlWorkFlowView>, objdbmlWorkFlowView);
                //}
                //else
                {
                    returndbmlWorkFlowView objreturndbmlWorkFlowView = objServiceClient.WorkFlowViewGetByBPId(intBPId, intDocId);
                    if (objreturndbmlWorkFlowView.objdbmlStatus.StatusId == 1 && objreturndbmlWorkFlowView.objdbmlWorkFlowView.Count > 0)
                    {
                        Session["WorkFlowView"] = objreturndbmlWorkFlowView.objdbmlWorkFlowView;
                        objdbmlWorkFlowView = objreturndbmlWorkFlowView.objdbmlWorkFlowView;
                    }
                }
            }
            catch
            {

            }
            return objdbmlWorkFlowView;
        }

        [ValidateAntiForgeryToken]
        public ActionResult AcceptPI(int intQuotFlag)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlBooking objreturndbmlBooking = new returndbmlBooking();

            try
            {

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    returndbmlStatus objreturndbmlStatus = new returndbmlStatus();
                    objreturndbmlStatus.objdbmlStatus = new dbmlStatus();
                    objreturndbmlStatus.objdbmlStatus.StatusId = 1;
                    if (intQuotFlag == 1)
                    {
                        objreturndbmlStatus = objServiceClient.BookingQuotationPIDetailInsertByBookingId(objdbmlBooking.BookingId);
                    }
                    if (objreturndbmlStatus.objdbmlStatus.StatusId == 1)
                    {
                        objreturndbmlBooking = objServiceClient.WorkFlowActivityInsert(objdbmlBooking.BookingId, Convert.ToInt32(Session["BPId"]), Convert.ToInt32(objdbmlBooking.ZZWorkFlowId), Convert.ToInt32(HardCodeValues.SubmitStatusId), "", Convert.ToInt32(Session["UserId"]));
                        if (objreturndbmlBooking != null && objreturndbmlBooking.objdbmlStatus.StatusId == 1)
                        {
                            Session["objdbmlBooking"] = objreturndbmlBooking.objdbmlBookingList.FirstOrDefault();
                            intStatusId = 1;
                            strStatus = "Data Saved Successfully";
                        }
                        else
                        {
                            strStatus = objreturndbmlBooking.objdbmlStatus.Status;
                        }
                    }
                    else
                    {
                        strStatus = objreturndbmlBooking.objdbmlStatus.Status;
                    }
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, BookingList = objreturndbmlBooking.objdbmlBookingList }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult BookingQuotationPI()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlBooking objreturndbmlBooking = new returndbmlBooking();

            try
            {

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    returndbmlStatus objreturndbmlStatus = objServiceClient.BookingQuotationPIDetailInsertByBookingId(objdbmlBooking.BookingId);
                    if (objreturndbmlStatus.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Success";
                    }
                    else
                    {
                        strStatus = objreturndbmlBooking.objdbmlStatus.Status;
                    }
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult POUpload()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }
            returndbmlBooking objreturndbmlBooking = new returndbmlBooking();
            int intStatusId = 99;
            string strStatus = "Invalid";

            try
            {

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    HttpPostedFileBase file = Request.Files["ImageData"];
                    if (file.InputStream.Length > 0)
                    {
                        string strFileExtention = file.ContentType;
                        if (strFileExtention.ToLower() == "image/jpeg" || strFileExtention.ToLower() == "application/pdf")
                        {
                            if ((file.ContentLength / 1024) > 0 && (file.ContentLength / 1024) <= 2048)
                            {
                                strFileExtention = strFileExtention.Substring(strFileExtention.IndexOf('/') + 1);
                                byte[] byteImage = objClassUserFunctions.ConvertToBytes(file.InputStream);
                                bool blnStatus = false;
                                string strFTPUserName = System.Configuration.ConfigurationManager.AppSettings["strFTPUserName"];
                                string strFTPUserPSW = System.Configuration.ConfigurationManager.AppSettings["strFTPUserPassword"];
                                string strFTPUrl = System.Configuration.ConfigurationManager.AppSettings["strFTPServer"];
                                string strFTPRoot = System.Configuration.ConfigurationManager.AppSettings["strFTPRoot"];

                                string strBlobAccount = System.Configuration.ConfigurationManager.AppSettings["strBlobAccount"];
                                string strAccountKey = System.Configuration.ConfigurationManager.AppSettings["strAccountKey"];
                                string strFileStorage = System.Configuration.ConfigurationManager.AppSettings["strFileStorage"];

                                string strImageName = "PO_" + Convert.ToString(objdbmlBooking.BookingId) + "." + strFileExtention;
                                string strImageContainerName = "natraximage";
                                string strImageURL = "";
                                string strFTPFilePath = "";
                                if (strFileStorage == "FTP")
                                {
                                    ////////////////////// For FTP /////////////////////////////////////////////                       
                                    strFTPFilePath = strFTPRoot + strImageName;

                                    blnStatus = objClassUserFunctions.UploadImageToFTPFromWEBCLIENT(strFTPUrl, strFTPUserName, strFTPUserPSW, byteImage, strFTPFilePath);
                                }
                                else if (strFileStorage == "AzureBlob")
                                {
                                    /////////////////////// For Azure Blob ///////////////////////////////////////////////////                            
                                    strImageURL = objClassUserFunctions.UploadFileStreamToAzureBlob(strBlobAccount, strAccountKey, strImageContainerName, strImageName, byteImage);
                                    if (strImageURL != "")
                                        blnStatus = true;
                                }
                                if (blnStatus)
                                {
                                    objdbmlBooking.PODocPath = strImageName;
                                    objdbmlBooking.UpdateId = Convert.ToInt32(Session["UserId"]);
                                    objdbmlBooking.UpdateDate = DateTime.Now;

                                    returndbmlBooking objreturndbmlBookingTemp = new returndbmlBooking();
                                    ObservableCollection<dbmlBookingView> objdbmlBookingViewList = new ObservableCollection<dbmlBookingView>();
                                    objdbmlBookingViewList.Add(objdbmlBooking);
                                    objreturndbmlBookingTemp.objdbmlBookingList = objdbmlBookingViewList;

                                    objreturndbmlBooking = objServiceClient.BookingUpdate(objreturndbmlBookingTemp);
                                    if (objreturndbmlBooking != null && objreturndbmlBooking.objdbmlStatus.StatusId == 1)
                                    {
                                        Session["objdbmlBooking"] = objreturndbmlBooking.objdbmlBookingList.FirstOrDefault();
                                        intStatusId = 1;
                                        strStatus = "PO Uploaded Successfully";
                                    }
                                    else
                                    {
                                        if (strFileStorage == "FTP")
                                        {
                                            bool delStatus = objClassUserFunctions.DeleteFileFromFTPFromWEBCLIENT(strFTPUrl, strFTPUserName, strFTPUserPSW, strFTPFilePath);
                                        }
                                        else if (strFileStorage == "AzureBlob")
                                        {
                                            bool delStatus = objClassUserFunctions.DeleteFileFromAzureBlob(strBlobAccount, strAccountKey, strImageContainerName, strImageName);
                                        }

                                        strStatus = "PO Uploading Process Failed!";
                                        //strStatus = objreturndbmlBooking.objdbmlStatus.Status;
                                    }
                                }
                                else
                                {
                                    strStatus = "PO Uploading Process Failed!";
                                }
                            }
                            else
                            {
                                strStatus = "PO File Size between 1KB to 2 MB can be accepted!";
                            }
                        }
                        else
                        {
                            strStatus = "Only (JPEG, PDF) format can be accepted!";
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }

            return Json(new { StatusId = intStatusId, Status = strStatus }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult LoadServiceDates()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlServiceDateViewFront objreturndbmlServiceDateViewFront = new returndbmlServiceDateViewFront();
            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    objreturndbmlServiceDateViewFront = objServiceClient.ServiceDateViewFrontGetByBookingId(objdbmlBooking.BookingId);
                    if (objreturndbmlServiceDateViewFront.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Success";
                    }
                    else
                    {
                        strStatus = objreturndbmlServiceDateViewFront.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, ServiceDateList = objreturndbmlServiceDateViewFront.objdbmlServiceDateViewFront }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult UpdateServiceDates(ObservableCollection<dbmlServiceDateViewFront> model)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlServiceDateViewFront objreturndbmlServiceDateViewFront = new returndbmlServiceDateViewFront();
            objreturndbmlServiceDateViewFront.objdbmlServiceDateViewFront = model;
            try
            {
                if (Session["objdbmlBooking"] != null && model != null && model.Count > 0)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    returndbmlBooking objreturndbmlBooking = objServiceClient.UpdateServiceDateFrontByBookingIdDayDates(objreturndbmlServiceDateViewFront);

                    if (objreturndbmlBooking != null && objreturndbmlBooking.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Service Dates Updated Successfully";
                    }
                    else
                    {
                        strStatus = objreturndbmlBooking.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Vehicle

        public ActionResult Vehicle()
        {
            CommonModel model = new CommonModel();
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);
                model.StatusPropId = 0;
                model.StateId = Convert.ToInt32(Session["StateId"]);
                ViewBag.VehicleType = GetVehicleType();

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = Convert.ToInt32(objdbmlBooking.ZZWorkFlowId);
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;
                    model.StatusPropId = Convert.ToInt32(objdbmlBooking.StatusPropId);
                    model.BPId = Convert.ToInt32(Session["BPId"]);
                    model.ReportURL = strRptURL;
                    model.DocId = objdbmlBooking.BookingId;
                    model.POURL = strPOURL + objdbmlBooking.PODocPath;

                    //if (Convert.ToInt32(objdbmlBooking.TabStatusId) + 10 < 10)
                    //{
                    //    return RedirectToActionByStatusId(Convert.ToInt32(objdbmlBooking.TabStatusId));
                    //}
                    model.WorkFlowView = WorkFlowViewGetByBPId(Convert.ToInt32(Session["BPId"]), objdbmlBooking.BookingId);
                    model.RFQId = Convert.ToInt32(objdbmlBooking.RFQId);
                    model.RFQBPId = Convert.ToInt32(objdbmlBooking.ZZRFQBPId);
                    model.RFQBookingNo = objdbmlBooking.ZZRFQBookingNo;
                }
                else
                {
                    return RedirectToAction("Basic", "Home");
                }
            }
            catch
            {
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Vehicle(CommonModel model, string btnPrevNext)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                if (btnPrevNext.ToLower() == "prev")
                {
                    return RedirectToAction("Basic", "Home");
                }

                if (btnPrevNext.ToLower() == "next")
                {
                    if (Convert.ToInt32(Session["BPId"]) == 90 || Convert.ToInt32(Session["BPId"]) == 91)
                    {
                        return RedirectToAction("Component", "Home");
                    }
                    else
                    {
                        return RedirectToAction("MainTrackBooking", "Home");
                    }
                }
            }
            catch
            {
            }

            return RedirectToAction("Vehicle", "Home");
        }

        [ValidateAntiForgeryToken]
        public ActionResult LoadVehicleInfo()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlListOfVehicleComponent objreturndbmlVehicle = new returndbmlListOfVehicleComponent();
            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    objreturndbmlVehicle = objServiceClient.ListOfVehicleComponentGetByDocId(objdbmlBooking.BookingId);
                    if (objreturndbmlVehicle.objdbmlStatus.StatusId == 1)
                    {
                        objreturndbmlVehicle.objdbmlListOfVehicleComponent = new ObservableCollection<dbmlListOfVehicleComponent>(objreturndbmlVehicle.objdbmlListOfVehicleComponent.Where(itm => itm.GroupId == Convert.ToInt32(HardCodeValues.VehicleGrpPropId)));
                        intStatusId = 1;
                        strStatus = "Success";
                    }
                    else
                    {
                        strStatus = objreturndbmlVehicle.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, VehicleList = objreturndbmlVehicle.objdbmlListOfVehicleComponent }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult VehicleSave(dbmlListOfVehicleComponent model)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlListOfVehicleComponent objreturndbmlVehicle = new returndbmlListOfVehicleComponent();

            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    model.DocId = objdbmlBooking.BookingId;
                    model.BPId = Convert.ToInt32(Session["BPId"]);
                    model.GroupId = Convert.ToInt32(HardCodeValues.VehicleGrpPropId);
                    model.GroupName = "Vehicle";
                    model.CreateId = Convert.ToInt32(Session["UserId"]);
                    model.CreateDate = DateTime.Now;
                    model.UpdateId = Convert.ToInt32(Session["UserId"]);
                    model.UpdateDate = DateTime.Now;

                    returndbmlListOfVehicleComponent objreturndbmlVehicleTemp = new returndbmlListOfVehicleComponent();
                    ObservableCollection<dbmlListOfVehicleComponent> objdbmlVehicle = new ObservableCollection<dbmlListOfVehicleComponent>();
                    objdbmlVehicle.Add(model);
                    objreturndbmlVehicleTemp.objdbmlListOfVehicleComponent = objdbmlVehicle;

                    if (model.VehCompId < 0)
                    {
                        objreturndbmlVehicle = objServiceClient.ListOfVehicleComponentInsert(objreturndbmlVehicleTemp);
                    }
                    else
                    {
                        objreturndbmlVehicle = objServiceClient.ListOfVehicleComponentUpdate(objreturndbmlVehicleTemp);
                    }

                    if (objreturndbmlVehicle != null && objreturndbmlVehicle.objdbmlStatus.StatusId == 1)
                    {
                        objreturndbmlVehicle.objdbmlListOfVehicleComponent = new ObservableCollection<dbmlListOfVehicleComponent>(objreturndbmlVehicle.objdbmlListOfVehicleComponent.Where(itm => itm.GroupId == Convert.ToInt32(HardCodeValues.VehicleGrpPropId)));
                        intStatusId = 1;
                        strStatus = "Data Saved Successfully";
                    }
                    else
                    {
                        strStatus = objreturndbmlVehicle.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, VehicleList = objreturndbmlVehicle.objdbmlListOfVehicleComponent }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult VehicleDelete(int intVehCompId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlListOfVehicleComponent objreturndbmlVehicle = new returndbmlListOfVehicleComponent();

            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    objreturndbmlVehicle = objServiceClient.ListOfVehicleComponentDeleteByDocIdCompId(objdbmlBooking.BookingId, intVehCompId);

                    if (objreturndbmlVehicle != null && objreturndbmlVehicle.objdbmlStatus.StatusId == 1)
                    {
                        objreturndbmlVehicle.objdbmlListOfVehicleComponent = new ObservableCollection<dbmlListOfVehicleComponent>(objreturndbmlVehicle.objdbmlListOfVehicleComponent.Where(itm => itm.GroupId == Convert.ToInt32(HardCodeValues.VehicleGrpPropId)));
                        intStatusId = 1;
                        strStatus = "Vehicle Deleted Successfully";
                    }
                    else
                    {
                        strStatus = objreturndbmlVehicle.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, VehicleList = objreturndbmlVehicle.objdbmlListOfVehicleComponent }, JsonRequestBehavior.AllowGet);
        }

        public List<SelectListItem> GetVehicleType()
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                ObservableCollection<dbmlProperty> objProp = new ObservableCollection<dbmlProperty>();
                if (Session["Properties"] != null)
                {
                    GeneralColl<dbmlProperty>.CopyCollection(Session["Properties"] as ObservableCollection<dbmlProperty>, objProp);
                }
                else
                {
                    returndbmlProperty objreturndbmlProperty = objServiceClient.PropertiesGetAll();
                    if (objreturndbmlProperty.objdbmlStatus.StatusId == 1 && objreturndbmlProperty.objdbmlProperty.Count > 0)
                    {
                        Session["Properties"] = objreturndbmlProperty.objdbmlProperty;
                        objProp = objreturndbmlProperty.objdbmlProperty;
                    }
                }

                if (objProp != null && objProp.Count > 0)
                {
                    ObservableCollection<dbmlProperty> objPropList = new ObservableCollection<dbmlProperty>(objProp.Where(itm => itm.PropertyTypeId == Convert.ToInt32(HardCodeValues.VehicleTypePropId)));
                    foreach (var itm in objPropList)
                    {
                        Items.Add(new SelectListItem { Text = itm.Property, Value = itm.PropertyId.ToString(), Selected = false });
                    }
                }
            }
            catch
            {

            }
            return Items;
        }
        #endregion

        #region Component

        public ActionResult Component()
        {
            CommonModel model = new CommonModel();
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);
                model.StatusPropId = 0;
                model.StateId = Convert.ToInt32(Session["StateId"]);
                ViewBag.VehicleType = GetComponentType();

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = Convert.ToInt32(objdbmlBooking.ZZWorkFlowId);
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;
                    model.StatusPropId = Convert.ToInt32(objdbmlBooking.StatusPropId);
                    model.BPId = Convert.ToInt32(Session["BPId"]);
                    model.ReportURL = strRptURL;
                    model.DocId = objdbmlBooking.BookingId;
                    model.POURL = strPOURL + objdbmlBooking.PODocPath;

                    //if (Convert.ToInt32(objdbmlBooking.TabStatusId) + 10 < 10)
                    //{
                    //    return RedirectToActionByStatusId(Convert.ToInt32(objdbmlBooking.TabStatusId));
                    //}
                    model.WorkFlowView = WorkFlowViewGetByBPId(Convert.ToInt32(Session["BPId"]), objdbmlBooking.BookingId);
                    model.RFQId = Convert.ToInt32(objdbmlBooking.RFQId);
                    model.RFQBPId = Convert.ToInt32(objdbmlBooking.ZZRFQBPId);
                    model.RFQBookingNo = objdbmlBooking.ZZRFQBookingNo;
                }
                else
                {
                    return RedirectToAction("Basic", "Home");
                }
            }
            catch
            {
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Component(CommonModel model, string btnPrevNext)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                if (btnPrevNext.ToLower() == "prev")
                {
                    return RedirectToAction("Vehicle", "Home");
                }

                if (btnPrevNext.ToLower() == "next")
                {
                    return RedirectToAction("MainLabBooking", "Home");
                }
            }
            catch
            {
            }

            return RedirectToAction("Vehicle", "Home");
        }

        [ValidateAntiForgeryToken]
        public ActionResult LoadComponentInfo()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlListOfVehicleComponent objreturndbmlVehicle = new returndbmlListOfVehicleComponent();
            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    objreturndbmlVehicle = objServiceClient.ListOfVehicleComponentGetByDocId(objdbmlBooking.BookingId);
                    if (objreturndbmlVehicle.objdbmlStatus.StatusId == 1)
                    {
                        objreturndbmlVehicle.objdbmlListOfVehicleComponent = new ObservableCollection<dbmlListOfVehicleComponent>(objreturndbmlVehicle.objdbmlListOfVehicleComponent.Where(itm => itm.GroupId == Convert.ToInt32(HardCodeValues.CompGrpPropId)));
                        intStatusId = 1;
                        strStatus = "Success";
                    }
                    else
                    {
                        strStatus = objreturndbmlVehicle.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, VehicleList = objreturndbmlVehicle.objdbmlListOfVehicleComponent }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult ComponentSave(dbmlListOfVehicleComponent model)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlListOfVehicleComponent objreturndbmlVehicle = new returndbmlListOfVehicleComponent();

            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    model.DocId = objdbmlBooking.BookingId;
                    model.BPId = Convert.ToInt32(Session["BPId"]);
                    model.GroupId = Convert.ToInt32(HardCodeValues.CompGrpPropId);
                    model.GroupName = "Component";
                    model.CreateId = Convert.ToInt32(Session["UserId"]);
                    model.CreateDate = DateTime.Now;
                    model.UpdateId = Convert.ToInt32(Session["UserId"]);
                    model.UpdateDate = DateTime.Now;

                    returndbmlListOfVehicleComponent objreturndbmlVehicleTemp = new returndbmlListOfVehicleComponent();
                    ObservableCollection<dbmlListOfVehicleComponent> objdbmlVehicle = new ObservableCollection<dbmlListOfVehicleComponent>();
                    objdbmlVehicle.Add(model);
                    objreturndbmlVehicleTemp.objdbmlListOfVehicleComponent = objdbmlVehicle;

                    if (model.VehCompId < 0)
                    {
                        objreturndbmlVehicle = objServiceClient.ListOfVehicleComponentInsert(objreturndbmlVehicleTemp);
                    }
                    else
                    {
                        objreturndbmlVehicle = objServiceClient.ListOfVehicleComponentUpdate(objreturndbmlVehicleTemp);
                    }

                    if (objreturndbmlVehicle != null && objreturndbmlVehicle.objdbmlStatus.StatusId == 1)
                    {
                        objreturndbmlVehicle.objdbmlListOfVehicleComponent = new ObservableCollection<dbmlListOfVehicleComponent>(objreturndbmlVehicle.objdbmlListOfVehicleComponent.Where(itm => itm.GroupId == Convert.ToInt32(HardCodeValues.CompGrpPropId)));
                        intStatusId = 1;
                        strStatus = "Data Saved Successfully";
                    }
                    else
                    {
                        strStatus = objreturndbmlVehicle.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, VehicleList = objreturndbmlVehicle.objdbmlListOfVehicleComponent }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult ComponentDelete(int intVehCompId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlListOfVehicleComponent objreturndbmlVehicle = new returndbmlListOfVehicleComponent();

            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    objreturndbmlVehicle = objServiceClient.ListOfVehicleComponentDeleteByDocIdCompId(objdbmlBooking.BookingId, intVehCompId);

                    if (objreturndbmlVehicle != null && objreturndbmlVehicle.objdbmlStatus.StatusId == 1)
                    {
                        objreturndbmlVehicle.objdbmlListOfVehicleComponent = new ObservableCollection<dbmlListOfVehicleComponent>(objreturndbmlVehicle.objdbmlListOfVehicleComponent.Where(itm => itm.GroupId == Convert.ToInt32(HardCodeValues.CompGrpPropId)));
                        intStatusId = 1;
                        strStatus = "Component Deleted Successfully";
                    }
                    else
                    {
                        strStatus = objreturndbmlVehicle.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, VehicleList = objreturndbmlVehicle.objdbmlListOfVehicleComponent }, JsonRequestBehavior.AllowGet);
        }

        public List<SelectListItem> GetComponentType()
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                returndbmlOptionList objreturndbmlOptionList = objServiceClient.OptionListGetByPropertyId(Convert.ToInt32(HardCodeValues.CompOptPropId));
                if (objreturndbmlOptionList.objdbmlStatus.StatusId == 1 && objreturndbmlOptionList.objdbmlOptionList.Count > 0)
                {
                    foreach (var itm in objreturndbmlOptionList.objdbmlOptionList)
                    {
                        Items.Add(new SelectListItem { Text = itm.OptionName, Value = itm.OptionListId.ToString(), Selected = false });
                    }
                }
            }
            catch
            {

            }
            return Items;
        }
        #endregion

        #region Driver
        public ActionResult Driver()
        {
            CommonModel model = new CommonModel();
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);
                model.StateId = Convert.ToInt32(Session["StateId"]);

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = Convert.ToInt32(objdbmlBooking.ZZWorkFlowId);
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;
                    model.StatusPropId = Convert.ToInt32(objdbmlBooking.StatusPropId);
                    model.DocId = objdbmlBooking.BookingId;

                    //if (Convert.ToInt32(objdbmlBooking.TabStatusId) + 10 < 20)
                    //{
                    //    return RedirectToActionByStatusId(Convert.ToInt32(objdbmlBooking.TabStatusId));
                    //}
                    model.WorkFlowView = WorkFlowViewGetByBPId(Convert.ToInt32(Session["BPId"]), objdbmlBooking.BookingId);
                }
                else
                {
                    return RedirectToAction("Basic", "Home");
                }
            }
            catch
            {
            }

            return View(model);
        }
        #endregion

        #region Attendee
        public ActionResult Attendee()
        {
            CommonModel model = new CommonModel();
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);
                model.StateId = Convert.ToInt32(Session["StateId"]);

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = Convert.ToInt32(objdbmlBooking.ZZWorkFlowId);
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;
                    model.StatusPropId = Convert.ToInt32(objdbmlBooking.StatusPropId);
                    model.DocId = objdbmlBooking.BookingId;

                    //if (Convert.ToInt32(objdbmlBooking.TabStatusId) + 10 < 30)
                    //{
                    //    return RedirectToActionByStatusId(Convert.ToInt32(objdbmlBooking.TabStatusId));
                    //}
                    model.WorkFlowView = WorkFlowViewGetByBPId(Convert.ToInt32(Session["BPId"]), objdbmlBooking.BookingId);
                }
                else
                {
                    return RedirectToAction("Basic", "Home");
                }
            }
            catch
            {
            }

            return View(model);
        }
        #endregion

        #region Tracks
        public ActionResult MainTrackBooking()
        {
            CommonModel model = new CommonModel();
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);
                model.TrackGroup = "Track Booking";
                model.ViewTitle = "Track Booking";
                model.StateId = Convert.ToInt32(Session["StateId"]);

                returndbmlServicesView objreturndbmlServicesView = objServiceClient.ServicesGetByBPId(Convert.ToInt32(HardCodeValues.ServiceBPIdTrack));
                if (objreturndbmlServicesView.objdbmlStatus.StatusId == 1 && objreturndbmlServicesView.objdbmlServicesView.Count > 0)
                {
                    Session["Services"] = objreturndbmlServicesView.objdbmlServicesView;
                }

                //Session["TrackGroupId"] = model.TrackGroupId;
                //Session["TrackGroup"] = model.TrackGroup;
                ViewBag.ServiveLookup = GetServiveLookup(Convert.ToInt32(HardCodeValues.ServiceBPIdTrack));
                ViewBag.TimeSlot = GetTimeSlot();
                ViewBag.ServiveCategory = GetServiveCategory(model.TrackGroupId);

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = Convert.ToInt32(objdbmlBooking.ZZWorkFlowId);
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;
                    model.StatusPropId = Convert.ToInt32(objdbmlBooking.StatusPropId);
                    model.BPId = Convert.ToInt32(Session["BPId"]);
                    model.ReportURL = strRptURL;
                    model.DocId = objdbmlBooking.BookingId;
                    model.POURL = strPOURL + objdbmlBooking.PODocPath;

                    //if (Convert.ToInt32(objdbmlBooking.TabStatusId) + 10 < 40)
                    //{
                    //    return RedirectToActionByStatusId(Convert.ToInt32(objdbmlBooking.TabStatusId));
                    //}
                    model.WorkFlowView = WorkFlowViewGetByBPId(Convert.ToInt32(Session["BPId"]), objdbmlBooking.BookingId);
                    model.RFQId = Convert.ToInt32(objdbmlBooking.RFQId);
                    model.RFQBPId = Convert.ToInt32(objdbmlBooking.ZZRFQBPId);
                    model.RFQBookingNo = objdbmlBooking.ZZRFQBookingNo;
                }
                else
                {
                    return RedirectToAction("Basic", "Home");
                }

            }
            catch
            {
            }

            return View("MainTrackBooking", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MainTrackBooking(CommonModel model, string btnPrevNext)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                if (btnPrevNext.ToLower() == "prev")
                {
                    return RedirectToAction("Vehicle", "Home");
                }
                else if (btnPrevNext.ToLower() == "next")
                {
                    return RedirectToAction("TrackWorkshopBooking", "Home");
                }
            }
            catch
            {
            }

            return RedirectToAction("MainTrackBooking", "Home");
        }

        [ValidateAntiForgeryToken]
        public ActionResult LoadTrackInfo(int intTrackGroupId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";
            //int intTrackGroupId = Convert.ToInt32(Session["TrackGroupId"]);

            List<SelectListItem> lstCategory = GetServiveCategory(intTrackGroupId);

            ObservableCollection<dbmlServicesView> objdbmlServicesView = new ObservableCollection<dbmlServicesView>();
            if (Session["Services"] != null)
            {
                GeneralColl<dbmlServicesView>.CopyCollection(Session["Services"] as ObservableCollection<dbmlServicesView>, objdbmlServicesView);
            }
            if (intTrackGroupId > 0)
            {
                objdbmlServicesView = new ObservableCollection<dbmlServicesView>(objdbmlServicesView.Where(itm => itm.TrackGroupId == intTrackGroupId).OrderBy(itm => itm.SrNo));
            }
            else
            {
                objdbmlServicesView = new ObservableCollection<dbmlServicesView>(objdbmlServicesView.Where(itm => itm.TrackGroupId > 0).OrderBy(itm => itm.SrNo));
            }

            returndbmlTrackBookingDetail objreturndbmlTrackBookingDetail = new returndbmlTrackBookingDetail();
            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);


                    objreturndbmlTrackBookingDetail = objServiceClient.TrackBookingDetailGetByBookingIdTrackGroupId(objdbmlBooking.BookingId, intTrackGroupId);
                    if (objreturndbmlTrackBookingDetail.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Success";
                    }
                    else
                    {
                        strStatus = objreturndbmlTrackBookingDetail.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, ServiceCategoryList = lstCategory, ServicesList = objdbmlServicesView, TrackBookingDetailList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingDetail, TrackBookingTimeDetailList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingTimeDetail, TrackBookingTimeSummaryList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingTimeSummary }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult TrackBookingDetailSave(ObservableCollection<dbmlTrackBookingTimeDetail> model)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";
            //int intTrackGroupId = Convert.ToInt32(Session["TrackGroupId"]);

            returndbmlTrackBookingDetail objreturndbmlTrackBookingDetail = new returndbmlTrackBookingDetail();
            returndbmlTrackBookingDetail objreturndbmlTrackBookingDetailTemp = new returndbmlTrackBookingDetail();

            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    ObservableCollection<dbmlServicesView> objdbmlServicesView = new ObservableCollection<dbmlServicesView>();
                    if (Session["Services"] != null)
                    {
                        GeneralColl<dbmlServicesView>.CopyCollection(Session["Services"] as ObservableCollection<dbmlServicesView>, objdbmlServicesView);
                    }

                    objdbmlServicesView = new ObservableCollection<dbmlServicesView>(objdbmlServicesView.Where(itm => itm.ServiceId == model.FirstOrDefault().ServiceId));

                    if (Convert.ToString(objdbmlServicesView.FirstOrDefault().GroupforMinBillingHours).ToUpper() == "YES")
                    {
                        int intMinBillHrs = Convert.ToInt32(model.FirstOrDefault().BillingHrs);
                        int intGroupRoundHrs = 0;
                        int intItmCount = 0;
                        int intUsageHrs = -1;
                        int intUsageMin = -1;
                        int intFlag = 0;

                        foreach (var itm in model)
                        {
                            itm.BookingId = objdbmlBooking.BookingId;
                            itm.BookingDetailId = 0;
                            itm.BPId = Convert.ToInt32(HardCodeValues.ServiceBPIdTrack);
                            if (objdbmlBooking.BPId == 46 || objdbmlBooking.BPId == 91)
                            {
                                itm.Date = objdbmlBooking.BookingDate.AddDays(Convert.ToInt32(itm.BookingDay) - 1);
                            }
                            else
                            {
                                itm.Date = objClassUserFunctions.ToDateTimeNotNull(itm.ZZDate);
                            }

                            itm.CreateId = Convert.ToInt32(Session["UserId"]);
                            itm.CreateDate = DateTime.Now;
                            itm.UpdateId = Convert.ToInt32(Session["UserId"]);
                            itm.UpdateDate = DateTime.Now;

                            int intRoundOffHrs = Convert.ToInt32(itm.TotalHours);
                            int intRoundOffMin = Convert.ToInt32(itm.TotalMinutes);

                            if ((intRoundOffHrs > 0 && intRoundOffMin >= 30) || (intRoundOffHrs == 0 && intRoundOffMin >= 1))
                            {
                                intRoundOffHrs = intRoundOffHrs + 1;
                                intRoundOffMin = 0;
                            }

                            itm.RoundOffHrs = intRoundOffHrs;
                            itm.RoundOffMin = intRoundOffMin;
                            itm.BillingHrs = intRoundOffHrs;
                            intGroupRoundHrs += intRoundOffHrs;

                            itm.TotalHours = Convert.ToInt32(itm.TotalHours);
                            itm.TotalMinutes = Convert.ToInt32(itm.TotalMinutes);

                            if (intUsageHrs == Convert.ToInt32(itm.TotalHours) && intUsageMin == Convert.ToInt32(itm.TotalMinutes))
                            {
                                intFlag = 1;
                            }
                            else
                            {
                                intFlag = 0;
                            }
                            intUsageHrs = Convert.ToInt32(itm.TotalHours);
                            intUsageMin = Convert.ToInt32(itm.TotalMinutes);

                            intItmCount++;
                        }

                        if (intGroupRoundHrs < intMinBillHrs)
                        {
                            if (intItmCount == 2 && intFlag == 1 && (intMinBillHrs - intGroupRoundHrs) == 2)
                            {
                                model[0].BillingHrs += 1;
                                model[1].BillingHrs += 1;
                            }
                            else
                            {
                                int intServiceId = Convert.ToInt32(model.OrderByDescending(itm => Convert.ToInt32(itm.TotalHours)).ThenByDescending(itm => Convert.ToInt32(itm.TotalMinutes)).ThenBy(itm => Convert.ToDecimal(itm.Rate)).ThenBy(itm => Convert.ToInt32(itm.SrNo)).First().ServiceId);
                                model.FirstOrDefault(itm => itm.ServiceId == intServiceId).BillingHrs += (intMinBillHrs - intGroupRoundHrs);
                            }
                        }
                    }
                    else
                    {
                        foreach (var itm in model)
                        {
                            itm.BookingId = objdbmlBooking.BookingId;
                            itm.BookingDetailId = 0;
                            itm.BPId = Convert.ToInt32(HardCodeValues.ServiceBPIdTrack);
                            if (objdbmlBooking.BPId == 46 || objdbmlBooking.BPId == 91)
                            {
                                itm.Date = objdbmlBooking.BookingDate.AddDays(Convert.ToInt32(itm.BookingDay) - 1);
                            }
                            else
                            {
                                itm.Date = objClassUserFunctions.ToDateTimeNotNull(itm.ZZDate);
                            }
                            itm.CreateId = Convert.ToInt32(Session["UserId"]);
                            itm.CreateDate = DateTime.Now;
                            itm.UpdateId = Convert.ToInt32(Session["UserId"]);
                            itm.UpdateDate = DateTime.Now;

                            int intRoundOffHrs = Convert.ToInt32(itm.TotalHours);
                            int intRoundOffMin = Convert.ToInt32(itm.TotalMinutes);

                            if ((intRoundOffHrs > 0 && intRoundOffMin >= 30) || (intRoundOffHrs == 0 && intRoundOffMin >= 1))
                            {
                                intRoundOffHrs = intRoundOffHrs + 1;
                                intRoundOffMin = 0;
                            }

                            itm.RoundOffHrs = intRoundOffHrs;
                            itm.RoundOffMin = intRoundOffMin;

                            if (intRoundOffHrs > Convert.ToInt32(itm.BillingHrs))
                            {
                                itm.BillingHrs = intRoundOffHrs;
                            }
                        }
                    }

                    objreturndbmlTrackBookingDetailTemp.objdbmlTrackBookingTimeDetail = model;


                    objreturndbmlTrackBookingDetail = objServiceClient.TrackBookingDetailInsertFront(objreturndbmlTrackBookingDetailTemp);
                    if (objreturndbmlTrackBookingDetail.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Data Saved Successfully";
                    }
                    else
                    {
                        strStatus = objreturndbmlTrackBookingDetail.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, TrackBookingDetailList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingDetail, TrackBookingTimeDetailList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingTimeDetail, TrackBookingTimeSummaryList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingTimeSummary }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult BookingStatusGetByServiceIdTimeSlotPropIdWEFDate(ObservableCollection<int> intlstServiceId, int intTimeSlotId, string strWED)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlBookingStatusTimeSlotView objreturndbmlBookingStatusTimeSlotView = new returndbmlBookingStatusTimeSlotView();

            try
            {
                dbmlBookingView objdbmlBooking = new dbmlBookingView();
                GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                DateTime dtWED = DateTime.Now;
                if (objdbmlBooking.BPId == 46 || objdbmlBooking.BPId == 91)
                {
                    dtWED = objdbmlBooking.BookingDate.AddDays(Convert.ToInt32(strWED) - 1);
                }
                else
                {
                    dtWED = objClassUserFunctions.ToDateTimeNotNull(strWED);
                }

                objreturndbmlBookingStatusTimeSlotView = objServiceClient.BookingStatusGetByServiceIdTimeSlotPropIdWEFDate(intlstServiceId, intTimeSlotId, dtWED);

                if (objreturndbmlBookingStatusTimeSlotView != null && objreturndbmlBookingStatusTimeSlotView.objdbmlStatus.StatusId == 1)
                {
                    intStatusId = 1;
                    strStatus = "Success";
                }
                else
                {
                    strStatus = objreturndbmlBookingStatusTimeSlotView.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, BookingStatusList = objreturndbmlBookingStatusTimeSlotView.objdbmlBookingStatusTimeSlotView }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult TrackBookingDetailDelete(int intVehicleId, string strDate, int intServiceId, int intTimeSlotId, int intTrackGroupId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlTrackBookingDetail objreturndbmlTrackBookingDetail = new returndbmlTrackBookingDetail();

            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    //int intTrackGroupId = Convert.ToInt32(Session["TrackGroupId"]);
                    DateTime dtDate = objClassUserFunctions.ToDateTimeNotNull(strDate);

                    objreturndbmlTrackBookingDetail = objServiceClient.TrackBookingTimeDetailDeleteFrontByServiceId(objdbmlBooking.BookingId, intTrackGroupId, intVehicleId, dtDate, intServiceId, intTimeSlotId);
                    if (objreturndbmlTrackBookingDetail.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Data Deleted Successfully";
                    }
                    else
                    {
                        strStatus = objreturndbmlTrackBookingDetail.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, TrackBookingDetailList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingDetail, TrackBookingTimeDetailList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingTimeDetail, TrackBookingTimeSummaryList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingTimeSummary }, JsonRequestBehavior.AllowGet);
        }

        public List<SelectListItem> GetTimeSlot()
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                ObservableCollection<dbmlProperty> objProp = new ObservableCollection<dbmlProperty>();
                if (Session["Properties"] != null)
                {
                    GeneralColl<dbmlProperty>.CopyCollection(Session["Properties"] as ObservableCollection<dbmlProperty>, objProp);
                }
                else
                {
                    returndbmlProperty objreturndbmlProperty = objServiceClient.PropertiesGetAll();
                    if (objreturndbmlProperty.objdbmlStatus.StatusId == 1 && objreturndbmlProperty.objdbmlProperty.Count > 0)
                    {
                        Session["Properties"] = objreturndbmlProperty.objdbmlProperty;
                        objProp = objreturndbmlProperty.objdbmlProperty;
                    }
                }

                if (objProp != null && objProp.Count > 0)
                {
                    ObservableCollection<dbmlProperty> objPropList = new ObservableCollection<dbmlProperty>(objProp.Where(itm => itm.PropertyTypeId == Convert.ToInt32(HardCodeValues.TimeSlotPropId)));
                    foreach (var itm in objPropList)
                    {
                        Items.Add(new SelectListItem { Text = itm.Property, Value = itm.PropertyId.ToString(), Selected = false });
                    }
                }
            }
            catch
            {

            }
            return Items;
        }

        public List<SelectListItem> GetServiveCategory(int intTrackGroupId)
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                ObservableCollection<dbmlServicesView> objdbmlServicesView = new ObservableCollection<dbmlServicesView>();
                if (Session["Services"] != null)
                {
                    GeneralColl<dbmlServicesView>.CopyCollection(Session["Services"] as ObservableCollection<dbmlServicesView>, objdbmlServicesView);
                }
                else
                {
                    returndbmlServicesView objreturndbmlServicesView = objServiceClient.ServicesGetByBPId((int)HardCodeValues.ServiceBPIdTrack);
                    if (objreturndbmlServicesView.objdbmlStatus.StatusId == 1 && objreturndbmlServicesView.objdbmlServicesView.Count > 0)
                    {
                        Session["Services"] = objreturndbmlServicesView.objdbmlServicesView;
                        objdbmlServicesView = objreturndbmlServicesView.objdbmlServicesView;
                    }
                }

                if (objdbmlServicesView != null && objdbmlServicesView.Count > 0)
                {
                    ObservableCollection<dbmlServicesView> objdbmlServicesViewList = new ObservableCollection<dbmlServicesView>(objdbmlServicesView.Where(itm => itm.TrackGroupId == intTrackGroupId).OrderBy(itm => itm.SrNo));
                    foreach (var itm in objdbmlServicesViewList)
                    {
                        if (Items.FirstOrDefault(Category => Convert.ToInt32(Category.Value) == itm.CategoryPropId) == null)
                        {
                            Items.Add(new SelectListItem { Text = itm.Category, Value = itm.CategoryPropId.ToString(), Selected = false });
                        }
                    }
                }
            }
            catch
            {

            }
            return Items;
        }

        public List<SelectListItem> GetServiveLookup(int intBPId)
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                ObservableCollection<dbmlServicesView> objdbmlServicesView = new ObservableCollection<dbmlServicesView>();
                if (Session["Services"] != null)
                {
                    GeneralColl<dbmlServicesView>.CopyCollection(Session["Services"] as ObservableCollection<dbmlServicesView>, objdbmlServicesView);
                }
                else
                {
                    returndbmlServicesView objreturndbmlServicesView = objServiceClient.ServicesGetByBPId(intBPId);
                    if (objreturndbmlServicesView.objdbmlStatus.StatusId == 1 && objreturndbmlServicesView.objdbmlServicesView.Count > 0)
                    {
                        Session["Services"] = objreturndbmlServicesView.objdbmlServicesView;
                        objdbmlServicesView = objreturndbmlServicesView.objdbmlServicesView;
                    }
                }

                if (objdbmlServicesView != null && objdbmlServicesView.Count > 0)
                {
                    ObservableCollection<dbmlServicesView> objdbmlServicesViewList = new ObservableCollection<dbmlServicesView>(objdbmlServicesView.Where(itm => itm.TrackGroupId > 0).OrderBy(itm => itm.SrNo));
                    foreach (var itm in objdbmlServicesViewList)
                    {
                        if (Items.FirstOrDefault(itmTrack => Convert.ToInt32(itmTrack.Value) == itm.TrackGroupId) == null)
                        {
                            Items.Add(new SelectListItem { Text = itm.ZZTrackGroup, Value = itm.TrackGroupId.ToString(), Selected = false });
                        }
                    }
                }
            }
            catch
            {

            }
            return Items;
        }


        #endregion

        #region Workshop Booking

        public ActionResult TrackWorkshopBooking()
        {
            CommonModel model = new CommonModel();
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);
                model.TrackGroup = "Track Booking";
                model.ViewTitle = "Track Booking";
                model.StateId = Convert.ToInt32(Session["StateId"]);

                returndbmlServicesView objreturndbmlServicesView = objServiceClient.ServicesGetByBPId(Convert.ToInt32(HardCodeValues.ServiceBPIdWorkShop));
                if (objreturndbmlServicesView.objdbmlStatus.StatusId == 1 && objreturndbmlServicesView.objdbmlServicesView.Count > 0)
                {
                    Session["WorkShopServices"] = objreturndbmlServicesView.objdbmlServicesView;
                }

                ViewBag.ServiveLookup = GetWorkShopServiveLookup(Convert.ToInt32(HardCodeValues.ServiceBPIdWorkShop));

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = Convert.ToInt32(objdbmlBooking.ZZWorkFlowId);
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;
                    model.StatusPropId = Convert.ToInt32(objdbmlBooking.StatusPropId);
                    model.BPId = Convert.ToInt32(Session["BPId"]);
                    model.ReportURL = strRptURL;
                    model.DocId = objdbmlBooking.BookingId;
                    model.POURL = strPOURL + objdbmlBooking.PODocPath;

                    //if (Convert.ToInt32(objdbmlBooking.TabStatusId) + 10 < 40)
                    //{
                    //    return RedirectToActionByStatusId(Convert.ToInt32(objdbmlBooking.TabStatusId));
                    //}
                    model.WorkFlowView = WorkFlowViewGetByBPId(Convert.ToInt32(Session["BPId"]), objdbmlBooking.BookingId);
                    model.RFQId = Convert.ToInt32(objdbmlBooking.RFQId);
                    model.RFQBPId = Convert.ToInt32(objdbmlBooking.ZZRFQBPId);
                    model.RFQBookingNo = objdbmlBooking.ZZRFQBookingNo;
                }
                else
                {
                    return RedirectToAction("Basic", "Home");
                }

            }
            catch
            {
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TrackWorkshopBooking(CommonModel model, string btnPrevNext)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                if (btnPrevNext.ToLower() == "prev")
                {
                    if (Convert.ToInt32(Session["BPId"]) == 90 || Convert.ToInt32(Session["BPId"]) == 91)
                    {
                        return RedirectToAction("MainLabBooking", "Home");
                    }
                    else
                    {
                        return RedirectToAction("MainTrackBooking", "Home");
                    }

                }

                if (btnPrevNext.ToLower() == "next")
                {
                    return RedirectToAction("TrackAddOnServicesBooking", "Home");
                }
            }
            catch
            {
            }

            return RedirectToAction("TrackWorkshopBooking", "Home");
        }

        [ValidateAntiForgeryToken]
        public ActionResult LoadWorkshopBookingInfo()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlWorkshopBookingDetailViewFront objreturndbmlWorkshopBookingDetailViewFront = new returndbmlWorkshopBookingDetailViewFront();
            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    objreturndbmlWorkshopBookingDetailViewFront = objServiceClient.WorkshopBookingDetailViewFrontGetByBookingId(objdbmlBooking.BookingId);
                    if (objreturndbmlWorkshopBookingDetailViewFront.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Success";
                    }
                    else
                    {
                        strStatus = objreturndbmlWorkshopBookingDetailViewFront.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, WorkshopBookingDetailList = objreturndbmlWorkshopBookingDetailViewFront.objdbmlWorkshopBookingDetailViewFront }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult WorkshopBookingSave(dbmlWorkshopBookingDetailViewFront model)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlWorkshopBookingDetailViewFront objreturndbmlWorkshopBookingDetailViewFront = new returndbmlWorkshopBookingDetailViewFront();

            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    model.BookingId = objdbmlBooking.BookingId;
                    model.RefServiceBPId = Convert.ToInt32(HardCodeValues.ServiceBPIdWorkShop);
                    if (objdbmlBooking.BPId == 46 || objdbmlBooking.BPId == 91)
                    {
                        model.UsageDate = objdbmlBooking.BookingDate.AddDays(Convert.ToInt32(model.BookingDay) - 1);
                    }
                    else
                    {
                        model.UsageDate = objClassUserFunctions.ToDateTimeNotNull(model.ZZUsageDate);
                    }
                    model.CreateId = Convert.ToInt32(Session["UserId"]);
                    model.CreateDate = DateTime.Now;
                    model.UpdateId = Convert.ToInt32(Session["UserId"]);
                    model.UpdateDate = DateTime.Now;

                    returndbmlWorkshopBookingDetailViewFront objreturndbmlWorkshopBookingDetailViewFrontTemp = new returndbmlWorkshopBookingDetailViewFront();
                    ObservableCollection<dbmlWorkshopBookingDetailViewFront> objdbmlWorkshopBookingDetailViewFront = new ObservableCollection<dbmlWorkshopBookingDetailViewFront>();
                    objdbmlWorkshopBookingDetailViewFront.Add(model);
                    objreturndbmlWorkshopBookingDetailViewFrontTemp.objdbmlWorkshopBookingDetailViewFront = objdbmlWorkshopBookingDetailViewFront;

                    objreturndbmlWorkshopBookingDetailViewFront = objServiceClient.WorkshopBookingDetailInsertFront(objreturndbmlWorkshopBookingDetailViewFrontTemp);

                    if (objreturndbmlWorkshopBookingDetailViewFront != null && objreturndbmlWorkshopBookingDetailViewFront.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Workshop Detail Saved Successfully";
                    }
                    else
                    {
                        strStatus = objreturndbmlWorkshopBookingDetailViewFront.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, WorkshopBookingDetailList = objreturndbmlWorkshopBookingDetailViewFront.objdbmlWorkshopBookingDetailViewFront }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult WorkshopBookingDelete(int intWorkshopBookingDetailId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlWorkshopBookingDetailViewFront objreturndbmlWorkshopBookingDetailViewFront = new returndbmlWorkshopBookingDetailViewFront();

            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    objreturndbmlWorkshopBookingDetailViewFront = objServiceClient.WorkshopBookingDetailDelete(objdbmlBooking.BookingId, intWorkshopBookingDetailId);

                    if (objreturndbmlWorkshopBookingDetailViewFront != null && objreturndbmlWorkshopBookingDetailViewFront.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Workshop Details Deleted Successfully";
                    }
                    else
                    {
                        strStatus = objreturndbmlWorkshopBookingDetailViewFront.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, WorkshopBookingDetailList = objreturndbmlWorkshopBookingDetailViewFront.objdbmlWorkshopBookingDetailViewFront }, JsonRequestBehavior.AllowGet);
        }

        public List<SelectListItem> GetWorkShopServiveLookup(int intBPId)
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                ObservableCollection<dbmlServicesView> objdbmlServicesView = new ObservableCollection<dbmlServicesView>();
                if (Session["WorkShopServices"] != null)
                {
                    GeneralColl<dbmlServicesView>.CopyCollection(Session["WorkShopServices"] as ObservableCollection<dbmlServicesView>, objdbmlServicesView);
                }
                else
                {
                    returndbmlServicesView objreturndbmlServicesView = objServiceClient.ServicesGetByBPId(intBPId);
                    if (objreturndbmlServicesView.objdbmlStatus.StatusId == 1 && objreturndbmlServicesView.objdbmlServicesView.Count > 0)
                    {
                        Session["WorkShopServices"] = objreturndbmlServicesView.objdbmlServicesView;
                        objdbmlServicesView = objreturndbmlServicesView.objdbmlServicesView;
                    }
                }

                if (objdbmlServicesView != null && objdbmlServicesView.Count > 0)
                {
                    ObservableCollection<dbmlServicesView> objdbmlServicesViewList = new ObservableCollection<dbmlServicesView>(objdbmlServicesView.Where(itm => itm.ServiceId > 0).OrderBy(itm => itm.SrNo));
                    foreach (var itm in objdbmlServicesViewList)
                    {
                        if (Items.FirstOrDefault(itmTrack => Convert.ToInt32(itmTrack.Value) == itm.ServiceId) == null)
                        {
                            Items.Add(new SelectListItem { Text = itm.ServiceName + " " + itm.ServiceSpecification, Value = itm.ServiceId.ToString(), Selected = false });
                        }
                    }
                }
            }
            catch
            {

            }
            return Items;
        }


        #endregion

        #region AddOn Services Booking

        public ActionResult TrackAddOnServicesBooking()
        {
            CommonModel model = new CommonModel();
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);
                model.TrackGroup = "Track Booking";
                model.ViewTitle = "Track Booking";
                model.StateId = Convert.ToInt32(Session["StateId"]);

                returndbmlServicesView objreturndbmlServicesView = objServiceClient.ServicesGetByBPId(Convert.ToInt32(HardCodeValues.ServiceBPIdAddOn));
                if (objreturndbmlServicesView.objdbmlStatus.StatusId == 1 && objreturndbmlServicesView.objdbmlServicesView.Count > 0)
                {
                    Session["AddOnServices"] = objreturndbmlServicesView.objdbmlServicesView;
                }

                ViewBag.ServiveLookup = GetAddOnServiveLookup(Convert.ToInt32(HardCodeValues.ServiceBPIdAddOn));

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = Convert.ToInt32(objdbmlBooking.ZZWorkFlowId);
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;
                    model.StatusPropId = Convert.ToInt32(objdbmlBooking.StatusPropId);
                    model.BPId = Convert.ToInt32(Session["BPId"]);
                    model.ReportURL = strRptURL;
                    model.DocId = objdbmlBooking.BookingId;
                    model.POURL = strPOURL + objdbmlBooking.PODocPath;

                    //if (Convert.ToInt32(objdbmlBooking.TabStatusId) + 10 < 40)
                    //{
                    //    return RedirectToActionByStatusId(Convert.ToInt32(objdbmlBooking.TabStatusId));
                    //}
                    model.WorkFlowView = WorkFlowViewGetByBPId(Convert.ToInt32(Session["BPId"]), objdbmlBooking.BookingId);
                    model.RFQId = Convert.ToInt32(objdbmlBooking.RFQId);
                    model.RFQBPId = Convert.ToInt32(objdbmlBooking.ZZRFQBPId);
                    model.RFQBookingNo = objdbmlBooking.ZZRFQBookingNo;
                }
                else
                {
                    return RedirectToAction("Basic", "Home");
                }

            }
            catch
            {
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TrackAddOnServicesBooking(CommonModel model, string btnPrevNext)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                if (btnPrevNext.ToLower() == "prev")
                {
                    return RedirectToAction("TrackWorkshopBooking", "Home");
                }

                if (btnPrevNext.ToLower() == "next")
                {
                    return RedirectToAction("WorkflowActivity", "Home");
                }
            }
            catch
            {
            }

            return RedirectToAction("TrackWorkshopBooking", "Home");
        }

        [ValidateAntiForgeryToken]
        public ActionResult LoadAddOnServicesInfo()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlBookingDetailAddOnServicesViewFront objreturndbmlBookingDetailAddOnServicesViewFront = new returndbmlBookingDetailAddOnServicesViewFront();
            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    objreturndbmlBookingDetailAddOnServicesViewFront = objServiceClient.BookingDetailAddOnServicesViewFrontGetByBookingId(objdbmlBooking.BookingId);
                    if (objreturndbmlBookingDetailAddOnServicesViewFront.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Success";
                    }
                    else
                    {
                        strStatus = objreturndbmlBookingDetailAddOnServicesViewFront.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, LoadAddOnServicesList = objreturndbmlBookingDetailAddOnServicesViewFront.objdbmlBookingDetailAddOnServicesViewFront }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult AddOnServicesSave(dbmlBookingDetailAddOnServicesViewFront model)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlBookingDetailAddOnServicesViewFront objreturndbmlBookingDetailAddOnServicesViewFront = new returndbmlBookingDetailAddOnServicesViewFront();

            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    model.BookingId = objdbmlBooking.BookingId;
                    model.BPId = Convert.ToInt32(HardCodeValues.ServiceBPIdAddOn);
                    if (objdbmlBooking.BPId == 46 || objdbmlBooking.BPId == 91)
                    {
                        model.ServiceDate = objdbmlBooking.BookingDate.AddDays(Convert.ToInt32(model.BookingDay) - 1);
                    }
                    else
                    {
                        model.ServiceDate = objClassUserFunctions.ToDateTimeNotNull(model.ZZServiceDate);
                    }
                    model.CreateId = Convert.ToInt32(Session["UserId"]);
                    model.CreateDate = DateTime.Now;
                    model.UpdateId = Convert.ToInt32(Session["UserId"]);
                    model.UpdateDate = DateTime.Now;

                    returndbmlBookingDetailAddOnServicesViewFront objreturndbmlBookingDetailAddOnServicesViewFrontTemp = new returndbmlBookingDetailAddOnServicesViewFront();
                    ObservableCollection<dbmlBookingDetailAddOnServicesViewFront> objdbmlBookingDetailAddOnServicesViewFront = new ObservableCollection<dbmlBookingDetailAddOnServicesViewFront>();
                    objdbmlBookingDetailAddOnServicesViewFront.Add(model);
                    objreturndbmlBookingDetailAddOnServicesViewFrontTemp.objdbmlBookingDetailAddOnServicesViewFront = objdbmlBookingDetailAddOnServicesViewFront;

                    objreturndbmlBookingDetailAddOnServicesViewFront = objServiceClient.BookingDetailAddOnServicesInsertFront(objreturndbmlBookingDetailAddOnServicesViewFrontTemp);

                    if (objreturndbmlBookingDetailAddOnServicesViewFront != null && objreturndbmlBookingDetailAddOnServicesViewFront.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Addon Service Saved Successfully";
                    }
                    else
                    {
                        strStatus = objreturndbmlBookingDetailAddOnServicesViewFront.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, LoadAddOnServicesList = objreturndbmlBookingDetailAddOnServicesViewFront.objdbmlBookingDetailAddOnServicesViewFront }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult AddOnServicesDelete(int intBookingAddOnServiceId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlBookingDetailAddOnServicesViewFront objreturndbmlBookingDetailAddOnServicesViewFront = new returndbmlBookingDetailAddOnServicesViewFront();

            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    objreturndbmlBookingDetailAddOnServicesViewFront = objServiceClient.BookingDetailAddOnServicesDelete(objdbmlBooking.BookingId, intBookingAddOnServiceId);

                    if (objreturndbmlBookingDetailAddOnServicesViewFront != null && objreturndbmlBookingDetailAddOnServicesViewFront.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "AddOn Services Deleted Successfully";
                    }
                    else
                    {
                        strStatus = objreturndbmlBookingDetailAddOnServicesViewFront.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, LoadAddOnServicesList = objreturndbmlBookingDetailAddOnServicesViewFront.objdbmlBookingDetailAddOnServicesViewFront }, JsonRequestBehavior.AllowGet);
        }

        public List<SelectListItem> GetAddOnServiveLookup(int intBPId)
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                ObservableCollection<dbmlServicesView> objdbmlServicesView = new ObservableCollection<dbmlServicesView>();
                if (Session["AddOnServices"] != null)
                {
                    GeneralColl<dbmlServicesView>.CopyCollection(Session["AddOnServices"] as ObservableCollection<dbmlServicesView>, objdbmlServicesView);
                }
                else
                {
                    returndbmlServicesView objreturndbmlServicesView = objServiceClient.ServicesGetByBPId(intBPId);
                    if (objreturndbmlServicesView.objdbmlStatus.StatusId == 1 && objreturndbmlServicesView.objdbmlServicesView.Count > 0)
                    {
                        Session["AddOnServices"] = objreturndbmlServicesView.objdbmlServicesView;
                        objdbmlServicesView = objreturndbmlServicesView.objdbmlServicesView;
                    }
                }

                if (objdbmlServicesView != null && objdbmlServicesView.Count > 0)
                {
                    ObservableCollection<dbmlServicesView> objdbmlServicesViewList = new ObservableCollection<dbmlServicesView>(objdbmlServicesView.Where(itm => itm.ServiceId > 0).OrderBy(itm => itm.SrNo));
                    foreach (var itm in objdbmlServicesViewList)
                    {
                        if (Items.FirstOrDefault(itmTrack => Convert.ToInt32(itmTrack.Value) == itm.ServiceId) == null)
                        {
                            Items.Add(new SelectListItem { Text = itm.ServiceName + " " + itm.ServiceSpecification, Value = itm.ServiceId.ToString(), Selected = false });
                        }
                    }
                }
            }
            catch
            {

            }
            return Items;
        }


        #endregion

        #region Lab Booking Detail

        public ActionResult MainLabBooking()
        {
            CommonModel model = new CommonModel();
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);
                model.TrackGroup = "Track Booking";
                model.ViewTitle = "Track Booking";
                model.StateId = Convert.ToInt32(Session["StateId"]);

                returndbmlServicesView objreturndbmlServicesView = objServiceClient.ServicesGetByBPId(Convert.ToInt32(HardCodeValues.ServiceBPIdLab));
                if (objreturndbmlServicesView.objdbmlStatus.StatusId == 1 && objreturndbmlServicesView.objdbmlServicesView.Count > 0)
                {
                    Session["LabServices"] = objreturndbmlServicesView.objdbmlServicesView;
                }

                ViewBag.ServiveLookup = GetLabServiveLookup(Convert.ToInt32(HardCodeValues.ServiceBPIdAddOn));

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = Convert.ToInt32(objdbmlBooking.ZZWorkFlowId);
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;
                    model.StatusPropId = Convert.ToInt32(objdbmlBooking.StatusPropId);
                    model.BPId = Convert.ToInt32(Session["BPId"]);
                    model.ReportURL = strRptURL;
                    model.DocId = objdbmlBooking.BookingId;
                    model.POURL = strPOURL + objdbmlBooking.PODocPath;

                    //if (Convert.ToInt32(objdbmlBooking.TabStatusId) + 10 < 40)
                    //{
                    //    return RedirectToActionByStatusId(Convert.ToInt32(objdbmlBooking.TabStatusId));
                    //}
                    model.WorkFlowView = WorkFlowViewGetByBPId(Convert.ToInt32(Session["BPId"]), objdbmlBooking.BookingId);
                    model.RFQId = Convert.ToInt32(objdbmlBooking.RFQId);
                    model.RFQBPId = Convert.ToInt32(objdbmlBooking.ZZRFQBPId);
                    model.RFQBookingNo = objdbmlBooking.ZZRFQBookingNo;
                }
                else
                {
                    return RedirectToAction("Basic", "Home");
                }

            }
            catch
            {
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MainLabBooking(CommonModel model, string btnPrevNext)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                if (btnPrevNext.ToLower() == "prev")
                {
                    return RedirectToAction("Component", "Home");
                }

                if (btnPrevNext.ToLower() == "next")
                {
                    return RedirectToAction("TrackWorkshopBooking", "Home");
                }
            }
            catch
            {
            }

            return RedirectToAction("TrackWorkshopBooking", "Home");
        }

        [ValidateAntiForgeryToken]
        public ActionResult LoadLabServicesInfo()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlLabBookingDetailViewFront objreturndbmlLabBookingDetailViewFront = new returndbmlLabBookingDetailViewFront();
            ObservableCollection<dbmlServicesView> objdbmlServicesView = new ObservableCollection<dbmlServicesView>();
            ObservableCollection<dbmlLablinkVorC> objdbmlLablinkVorC = new ObservableCollection<dbmlLablinkVorC>();
            returndbmlListOfVehicleComponent objreturndbmlListOfVehicleComponent = new returndbmlListOfVehicleComponent();

            try
            {
                if (Session["LabServices"] != null)
                {
                    GeneralColl<dbmlServicesView>.CopyCollection(Session["LabServices"] as ObservableCollection<dbmlServicesView>, objdbmlServicesView);
                }

                if (Session["LablinkVorC"] != null)
                {
                    GeneralColl<dbmlLablinkVorC>.CopyCollection(Session["LablinkVorC"] as ObservableCollection<dbmlLablinkVorC>, objdbmlLablinkVorC);
                }

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    objreturndbmlLabBookingDetailViewFront = objServiceClient.LabBookingDetailViewFrontGetByBookingId(objdbmlBooking.BookingId);
                    if (objreturndbmlLabBookingDetailViewFront.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Success";
                    }
                    else
                    {
                        strStatus = objreturndbmlLabBookingDetailViewFront.objdbmlStatus.Status;
                    }

                    objreturndbmlListOfVehicleComponent = objServiceClient.ListOfVehicleComponentGetByDocId(objdbmlBooking.BookingId);

                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, ServiceList = objdbmlServicesView, LablinkVorCList = objdbmlLablinkVorC, VehicleCompList = objreturndbmlListOfVehicleComponent.objdbmlListOfVehicleComponent, LabServicesList = objreturndbmlLabBookingDetailViewFront.objdbmlLabBookingDetailViewFront }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult LabServicesSave(dbmlLabBookingDetailViewFront model)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlLabBookingDetailViewFront objreturndbmlLabBookingDetailViewFront = new returndbmlLabBookingDetailViewFront();

            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    model.BookingId = objdbmlBooking.BookingId;
                    model.RefServiceBPId = Convert.ToInt32(HardCodeValues.ServiceBPIdLab);
                    if (objdbmlBooking.BPId == 46 || objdbmlBooking.BPId == 91)
                    {
                        model.UsageDate = objdbmlBooking.BookingDate.AddDays(Convert.ToInt32(model.BookingDay) - 1);
                    }
                    else
                    {
                        model.UsageDate = objClassUserFunctions.ToDateTimeNotNull(model.ZZUsageDate);
                    }

                    model.CreateId = Convert.ToInt32(Session["UserId"]);
                    model.CreateDate = DateTime.Now;
                    model.UpdateId = Convert.ToInt32(Session["UserId"]);
                    model.UpdateDate = DateTime.Now;

                    returndbmlLabBookingDetailViewFront objreturndbmlLabBookingDetailViewFrontTemp = new returndbmlLabBookingDetailViewFront();
                    ObservableCollection<dbmlLabBookingDetailViewFront> objdbmlLabBookingDetailViewFront = new ObservableCollection<dbmlLabBookingDetailViewFront>();
                    objdbmlLabBookingDetailViewFront.Add(model);
                    objreturndbmlLabBookingDetailViewFrontTemp.objdbmlLabBookingDetailViewFront = objdbmlLabBookingDetailViewFront;

                    objreturndbmlLabBookingDetailViewFront = objServiceClient.LabBookingDetailInsertFront(objreturndbmlLabBookingDetailViewFrontTemp);

                    if (objreturndbmlLabBookingDetailViewFront != null && objreturndbmlLabBookingDetailViewFront.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Lab Service Saved Successfully";
                    }
                    else
                    {
                        strStatus = objreturndbmlLabBookingDetailViewFront.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, LabServicesList = objreturndbmlLabBookingDetailViewFront.objdbmlLabBookingDetailViewFront }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult LabServicesDelete(int intLabBookingDetailId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlLabBookingDetailViewFront objreturndbmlLabBookingDetailViewFront = new returndbmlLabBookingDetailViewFront();

            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    objreturndbmlLabBookingDetailViewFront = objServiceClient.LabBookingDetailDelete(objdbmlBooking.BookingId, intLabBookingDetailId);

                    if (objreturndbmlLabBookingDetailViewFront != null && objreturndbmlLabBookingDetailViewFront.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Lab Service Deleted Successfully";
                    }
                    else
                    {
                        strStatus = objreturndbmlLabBookingDetailViewFront.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, LabServicesList = objreturndbmlLabBookingDetailViewFront.objdbmlLabBookingDetailViewFront }, JsonRequestBehavior.AllowGet);
        }

        public List<SelectListItem> GetLabServiveLookup(int intBPId)
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                ObservableCollection<dbmlServicesView> objdbmlServicesView = new ObservableCollection<dbmlServicesView>();
                if (Session["LabServices"] != null)
                {
                    GeneralColl<dbmlServicesView>.CopyCollection(Session["LabServices"] as ObservableCollection<dbmlServicesView>, objdbmlServicesView);
                }
                else
                {
                    returndbmlServicesView objreturndbmlServicesView = objServiceClient.ServicesGetByBPId(intBPId);
                    if (objreturndbmlServicesView.objdbmlStatus.StatusId == 1 && objreturndbmlServicesView.objdbmlServicesView.Count > 0)
                    {
                        Session["LabServices"] = objreturndbmlServicesView.objdbmlServicesView;
                        objdbmlServicesView = objreturndbmlServicesView.objdbmlServicesView;
                    }
                }

                if (objdbmlServicesView != null && objdbmlServicesView.Count > 0)
                {
                    ObservableCollection<dbmlServicesView> objdbmlServicesViewList = new ObservableCollection<dbmlServicesView>(objdbmlServicesView.Where(itm => itm.ServiceId > 0).OrderBy(itm => itm.SrNo));
                    foreach (var itm in objdbmlServicesViewList)
                    {
                        if (Items.FirstOrDefault(itmTrack => itmTrack.Value == itm.ServiceName) == null)
                        {
                            Items.Add(new SelectListItem { Text = itm.ServiceName, Value = itm.ServiceName, Selected = false });
                        }
                    }
                }
            }
            catch
            {

            }
            return Items;
        }


        #endregion

        #region Workflow Activity Track
        public ActionResult WorkflowActivity()
        {
            CommonModel model = new CommonModel();
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);
                model.StatusPropId = 0;
                model.StateId = Convert.ToInt32(Session["StateId"]);
                ViewBag.VehicleType = GetVehicleType();

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = Convert.ToInt32(objdbmlBooking.ZZWorkFlowId);
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;
                    model.StatusPropId = Convert.ToInt32(objdbmlBooking.StatusPropId);
                    model.BPId = Convert.ToInt32(Session["BPId"]);
                    model.ReportURL = strRptURL;
                    model.DocId = objdbmlBooking.BookingId;
                    model.POURL = strPOURL + objdbmlBooking.PODocPath;

                    //if (Convert.ToInt32(objdbmlBooking.TabStatusId) + 10 < 10)
                    //{
                    //    return RedirectToActionByStatusId(Convert.ToInt32(objdbmlBooking.TabStatusId));
                    //}
                    model.WorkFlowView = WorkFlowViewGetByBPId(Convert.ToInt32(Session["BPId"]), objdbmlBooking.BookingId);
                    model.RFQId = Convert.ToInt32(objdbmlBooking.RFQId);
                    model.RFQBPId = Convert.ToInt32(objdbmlBooking.ZZRFQBPId);
                    model.RFQBookingNo = objdbmlBooking.ZZRFQBookingNo;
                }
                else
                {
                    return RedirectToAction("Basic", "Home");
                }
            }
            catch
            {
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult WorkflowActivity(CommonModel model, string btnPrevNext)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                if (btnPrevNext.ToLower() == "prev")
                {
                    return RedirectToAction("TrackAddOnServicesBooking", "Home");
                }

            }
            catch
            {
            }

            return RedirectToAction("Vehicle", "Home");
        }

        [ValidateAntiForgeryToken]
        public ActionResult LoadWorkflowActivityInfo()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlWorkFlowActivityTrackView objreturndbmlWorkFlowActivityTrackView = new returndbmlWorkFlowActivityTrackView();
            try
            {
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);

                    objreturndbmlWorkFlowActivityTrackView = objServiceClient.WorkFlowActivityTrackGetByBPIdDocId(objdbmlBooking.BPId, objdbmlBooking.BookingId);
                    if (objreturndbmlWorkFlowActivityTrackView.objdbmlStatus.StatusId == 1)
                    {
                        intStatusId = 1;
                        strStatus = "Success";
                    }
                    else
                    {
                        strStatus = objreturndbmlWorkFlowActivityTrackView.objdbmlStatus.Status;
                    }
                }
                else
                {
                    strStatus = "Booking Details Not Found";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, WorkFlowActivityList = objreturndbmlWorkFlowActivityTrackView.objdbmlWorkFlowActivityTrackView }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #endregion

        #region Company/Department  
        public ActionResult VerifyeMail(string xyz, string abc, string lmn)
        {
            CommonModel model = new CommonModel();
            model.StatusPropId = -1;
            try
            {
                int intUserId = Convert.ToInt32(abc);
                returndbmlUser objreturndbmlUser = objServiceClient.UsereMailIdVerification(intUserId);
                if (objreturndbmlUser != null && objreturndbmlUser.objdbmlStatus.StatusId == 1 && objreturndbmlUser.objdbmlUserView.Count > 0)
                {
                    Session["UserView"] = objreturndbmlUser.objdbmlUserView.FirstOrDefault();
                    Session["UserIdTemp"] = objreturndbmlUser.objdbmlUserView.FirstOrDefault().UserId;
                    model.UserName = objreturndbmlUser.objdbmlUserView.FirstOrDefault().UserName;
                    model.LoginId = objreturndbmlUser.objdbmlUserView.FirstOrDefault().LoginId;
                    model.Message = objreturndbmlUser.objdbmlStatus.Status;
                    model.StatusPropId = 1;
                }
                else
                {
                    model.Message = objreturndbmlUser.objdbmlStatus.Status;
                    if (objreturndbmlUser.objdbmlStatus.StatusId == -2)
                    {
                        model.StatusPropId = -2;
                    }

                }
            }
            catch
            {
                model.Message = "Un-Authrized Access";
            }

            return View(model);
        }

        [ValidateAntiForgeryToken]
        public ActionResult CreatePassword(dbmlUserView model)
        {
            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlUser objreturndbmlUser = new returndbmlUser();
            try
            {
                string strPSW = DecryptStringAES(model.PassWord);

                objreturndbmlUser = objServiceClient.UserPaswordReset(Convert.ToInt32(Session["UserIdTemp"]), strPSW);
                if (objreturndbmlUser != null && objreturndbmlUser.objdbmlStatus.StatusId == 1)
                {
                    intStatusId = 1;
                    strStatus = "New Password successfully created. Your user activation is pending by Natrax";
                }
                else
                {
                    strStatus = objreturndbmlUser.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId }, JsonRequestBehavior.AllowGet);
        }

        public List<SelectListItem> LoadState()
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                returndbmlState objreturndbmlState = objServiceClient.StateGetAll();
                if (objreturndbmlState != null && objreturndbmlState.objdbmlStatus.StatusId == 1)
                {
                    foreach (var itm in objreturndbmlState.objdbmlState)
                    {
                        Items.Add(new SelectListItem { Text = itm.State, Value = itm.StateId.ToString(), Selected = false });
                    }
                }
            }
            catch
            {

            }
            return Items;
        }

        [ValidateAntiForgeryToken]
        public ActionResult LoadDistictByStateId(int intStateId)
        {
            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlDistrict objreturndbmlDistrict = new returndbmlDistrict();
            try
            {
                objreturndbmlDistrict = objServiceClient.DistrictGetByStateId(intStateId);
                if (objreturndbmlDistrict != null && objreturndbmlDistrict.objdbmlStatus.StatusId == 1)
                {
                    intStatusId = 1;
                }
                else
                {
                    strStatus = objreturndbmlDistrict.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, DistrictList = objreturndbmlDistrict.objdbmlDistrict }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Department()
        {
            CommonModel model = new CommonModel();
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);
                model.StateId = Convert.ToInt32(Session["StateId"]);
            }
            catch
            {
            }

            return View(model);
        }

        [ValidateAntiForgeryToken]
        public ActionResult LoadDepartmentInfo()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlCompanyDepartment objreturndbmlCompanyDepartment = new returndbmlCompanyDepartment();
            try
            {
                objreturndbmlCompanyDepartment = objServiceClient.CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
                if (objreturndbmlCompanyDepartment.objdbmlStatus.StatusId == 1)
                {
                    intStatusId = 1;
                    strStatus = "Success";
                }
                else
                {
                    strStatus = objreturndbmlCompanyDepartment.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, CompanyDepartmentList = objreturndbmlCompanyDepartment.objdbmlCompanyDepartment }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult DepartmentSave(dbmlCompanyDepartment model)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlCompanyDepartment objreturndbmlCompanyDepartment = new returndbmlCompanyDepartment();

            try
            {
                model.CustomerMasterId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.CreateId = Convert.ToInt32(Session["UserId"]);
                model.CreateDate = DateTime.Now;
                model.UpdateId = Convert.ToInt32(Session["UserId"]);
                model.UpdateDate = DateTime.Now;

                returndbmlCompanyDepartment objreturndbmlCompanyDepartmentTemp = new returndbmlCompanyDepartment();
                ObservableCollection<dbmlCompanyDepartment> objdbmlCompanyDepartment = new ObservableCollection<dbmlCompanyDepartment>();
                objdbmlCompanyDepartment.Add(model);
                objreturndbmlCompanyDepartmentTemp.objdbmlCompanyDepartment = objdbmlCompanyDepartment;
                if (model.CompanyDepartmentId <= 0)
                {
                    objreturndbmlCompanyDepartment = objServiceClient.CompanyDepartmentInsert(objreturndbmlCompanyDepartmentTemp);
                }
                else
                {
                    objreturndbmlCompanyDepartment = objServiceClient.CompanyDepartmentUpdate(objreturndbmlCompanyDepartmentTemp);
                }
                if (objreturndbmlCompanyDepartment != null && objreturndbmlCompanyDepartment.objdbmlStatus.StatusId == 1)
                {
                    intStatusId = 1;
                    strStatus = "Data Saved Successfully";
                }
                else
                {
                    strStatus = objreturndbmlCompanyDepartment.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, CompanyDepartmentList = objreturndbmlCompanyDepartment.objdbmlCompanyDepartment }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Users()
        {
            CommonModel model = new CommonModel();
            try
            {
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                model.UserId = Convert.ToInt32(Session["UserId"]);
                model.UserTypePropId = Convert.ToInt32(Session["UserTypePropId"]);
                model.ZZCompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.UserName = Convert.ToString(Session["UserName"]);
                model.EmailId = Convert.ToString(Session["EmailId"]);
                model.LoginId = Convert.ToString(Session["LoginId"]);
                model.ZZUserType = Convert.ToString(Session["ZZUserType"]);
                model.UserCode = Convert.ToString(Session["UserCode"]);
                model.StateId = Convert.ToInt32(Session["StateId"]);
                ViewBag.Department = LoadDepartment();
            }
            catch
            {
            }

            return View(model);
        }

        public List<SelectListItem> LoadDepartment()
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                returndbmlCompanyDepartment objreturndbmlCompanyDepartment = objServiceClient.CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));
                if (objreturndbmlCompanyDepartment != null && objreturndbmlCompanyDepartment.objdbmlStatus.StatusId == 1)
                {
                    foreach (var itm in objreturndbmlCompanyDepartment.objdbmlCompanyDepartment)
                    {
                        Items.Add(new SelectListItem { Text = itm.Department, Value = itm.CompanyDepartmentId.ToString(), Selected = false });
                    }
                }
            }
            catch
            {

            }
            return Items;
        }

        [ValidateAntiForgeryToken]
        public ActionResult LoadUserInfo()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlUser objreturndbmlUser = new returndbmlUser();
            try
            {
                objreturndbmlUser = objServiceClient.UserViewFrontGetByCompanyId(Convert.ToInt32(Session["ZZCompanyId"]));
                if (objreturndbmlUser.objdbmlStatus.StatusId == 1)
                {
                    intStatusId = 1;
                    strStatus = "Success";
                }
                else
                {
                    strStatus = objreturndbmlUser.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, UserList = objreturndbmlUser.objdbmlUserView }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult UserSave(dbmlUserView model)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            }

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlUser objreturndbmlUser = new returndbmlUser();

            try
            {
                model.CustomerMasterId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.CreateId = Convert.ToInt32(Session["UserId"]);
                model.CreateDate = DateTime.Now;
                model.UpdateId = Convert.ToInt32(Session["UserId"]);
                model.UpdateDate = DateTime.Now;

                returndbmlUser objreturndbmlUserTemp = new returndbmlUser();
                ObservableCollection<dbmlUserView> objdbmlUserView = new ObservableCollection<dbmlUserView>();
                objdbmlUserView.Add(model);
                objreturndbmlUserTemp.objdbmlUserView = objdbmlUserView;
                if (model.UserId <= 0)
                {
                    objreturndbmlUser = objServiceClient.UserInsert(objreturndbmlUserTemp);
                }
                else
                {
                    objreturndbmlUser = objServiceClient.UserUpdate(objreturndbmlUserTemp);
                }
                if (objreturndbmlUser != null && objreturndbmlUser.objdbmlStatus.StatusId == 1)
                {
                    intStatusId = 1;
                    strStatus = "Data Saved Successfully";
                }
                else
                {
                    strStatus = objreturndbmlUser.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, UserList = objreturndbmlUser.objdbmlUserView }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Common
        public ActionResult RedirectToActionByStatusId(int intTabStatusId)
        {
            int intBPId = Convert.ToInt32(Session["BPId"]);
            switch (intTabStatusId)
            {
                case 0: return RedirectToAction("Basic", "Home");
                case 10: return RedirectToAction("Vehicle", "Home");
                case 20: return RedirectToAction("Driver", "Home");
                case 30: return RedirectToAction("Attenee", "Home");
                case 40: return RedirectToAction("MainTrackBooking", "Home");

                default: return RedirectToAction("Basic", "Home");
            }
        }
        protected override JsonResult Json(object data, string contentType, System.Text.Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new JsonResult()
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = behavior,
                MaxJsonLength = Int32.MaxValue
            };
        }
        #endregion       

    }
}