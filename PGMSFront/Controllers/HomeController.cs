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
        public ActionResult Index(LoginModel model)
        {
            try
            {
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
                        Session["ZZCompanyId"] = objdbmlUserView.ZZCompanyId;
                        Session["UserName"] = objdbmlUserView.UserName;
                        Session["EmailId"] = objdbmlUserView.EmailId;
                        Session["LoginId"] = objdbmlUserView.LoginId;
                        Session["ZZUserType"] = objdbmlUserView.ZZUserType;
                        Session["UserCode"] = objdbmlUserView.UserCode;

                        returndbmlProperty objreturndbmlProperty = objServiceClient.PropertiesGetAll();
                        if (objreturndbmlProperty.objdbmlStatus.StatusId == 1 && objreturndbmlProperty.objdbmlProperty.Count > 0)
                        {
                            Session["Properties"] = objreturndbmlProperty.objdbmlProperty;
                        }

                        returndbmlServicesView objreturndbmlServicesView = objServiceClient.ServicesGetByBPId(12);
                        if (objreturndbmlServicesView.objdbmlStatus.StatusId == 1 && objreturndbmlServicesView.objdbmlServicesView.Count > 0)
                        {
                            Session["Services"] = objreturndbmlServicesView.objdbmlServicesView;
                        }

                        return RedirectToAction("Dashboard", "Home");

                    }
                    else
                    {
                        string strErrMSG = "Invalid User Name";

                        if (objreturndbmlUser.objdbmlStatus.StatusId == 10)
                            strErrMSG = "Invalid Password";

                        // model.Message = strErrMSG;
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

        #endregion

        #region Booking

        #region Booking Search/New/Inprocess
        public ActionResult Booking()
        {
            CommonModel model = new CommonModel();
            try
            {
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

        public ActionResult NewBooking()
        {
            CommonModel model = new CommonModel();
            try
            {
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
        public ActionResult BookingSearchViewGetByDepartmentBookinStatus(int intDepartmentId,int intBookingTypeId,int intStatusPropId)
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
                }
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
                Items.Add(new SelectListItem {Text= "Booking",Value= "21" });
                Items.Add(new SelectListItem { Text = "RFQ - Confidential", Value = "46" });
                Items.Add(new SelectListItem { Text = "RFQ - Regular", Value = "98" });
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

                ViewBag.CompanyDepartment = CompanyDepartmentGetByCustomerMasterId(Convert.ToInt32(Session["ZZCompanyId"]));

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = objdbmlBooking.ZZWorkFlowId;
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;
                }
                else
                {
                    model.DocDate = "To be allotted";
                    model.DocNo = "To be allotted";
                    model.DocType = "Booking";
                    model.WorkFlowId = 6;
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
                model.BPId = 21;
                model.CompanyId = Convert.ToInt32(Session["ZZCompanyId"]);
                model.CreateId = Convert.ToInt32(Session["UserId"]);
                model.CreateDate = DateTime.Now;
                model.UpdateId = Convert.ToInt32(Session["UserId"]);
                model.UpdateDate = DateTime.Now;
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
                ViewBag.VehicleType = GetVehicleType();
               
                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.StatusPropId = objdbmlBooking.StatusPropId;
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = objdbmlBooking.ZZWorkFlowId;
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;

                    //if (Convert.ToInt32(objdbmlBooking.TabStatusId) + 10 < 10)
                    //{
                    //    return RedirectToActionByStatusId(Convert.ToInt32(objdbmlBooking.TabStatusId));
                    //}
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
                    return RedirectToAction("MainTrackBooking", "Home");
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
                    model.BPId = 21;
                    model.Id = 99;
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
                        intStatusId = 1;
                        strStatus = "Vehicle Deleteed Successfully";
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
                    ObservableCollection<dbmlProperty> objPropList = new ObservableCollection<dbmlProperty>(objProp.Where(itm => itm.PropertyTypeId == 14));
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

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = objdbmlBooking.ZZWorkFlowId;
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;

                    //if (Convert.ToInt32(objdbmlBooking.TabStatusId) + 10 < 20)
                    //{
                    //    return RedirectToActionByStatusId(Convert.ToInt32(objdbmlBooking.TabStatusId));
                    //}
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

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = objdbmlBooking.ZZWorkFlowId;
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;

                    //if (Convert.ToInt32(objdbmlBooking.TabStatusId) + 10 < 30)
                    //{
                    //    return RedirectToActionByStatusId(Convert.ToInt32(objdbmlBooking.TabStatusId));
                    //}
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
                model.TrackGroupId = 2;
                model.TrackGroup = "T2-Dynamic Platform";
                model.ViewTitle = "T2 Dynamic Platform";

                Session["TrackGroupId"] = model.TrackGroupId;
                Session["TrackGroup"] = model.TrackGroup;
                ViewBag.ServiveLookup = GetServiveLookup();
                ViewBag.TimeSlot = GetTimeSlot();
                ViewBag.ServiveCategory = GetServiveCategory(model.TrackGroupId);

                if (Session["objdbmlBooking"] != null)
                {
                    dbmlBookingView objdbmlBooking = new dbmlBookingView();
                    GeneralColl<dbmlBookingView>.CopyObject(Session["objdbmlBooking"] as dbmlBookingView, objdbmlBooking);
                    model.DocDate = objdbmlBooking.ZZBookingDate;
                    model.DocNo = objdbmlBooking.BookingNo;
                    model.DocType = objdbmlBooking.ZZBookingType;
                    model.WorkFlowId = objdbmlBooking.ZZWorkFlowId;
                    model.WorkFlowStatusId = objdbmlBooking.ZZStatusWorkflowId;

                    //if (Convert.ToInt32(objdbmlBooking.TabStatusId) + 10 < 40)
                    //{
                    //    return RedirectToActionByStatusId(Convert.ToInt32(objdbmlBooking.TabStatusId));
                    //}
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
            }
            catch
            {
            }

            return RedirectToAction("MainTrackBooking", "Home");
        }

        public ActionResult HighSpeedTrack()
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
                model.TrackGroupId = 1;
                model.TrackGroup = "T1-High speed Track";
                model.ViewTitle = "T1-High speed Track";

                Session["TrackGroupId"] = model.TrackGroupId;
                Session["TrackGroup"] = model.TrackGroup;

                ViewBag.TimeSlot = GetTimeSlot();
                ViewBag.ServiveCategory = GetServiveCategory(model.TrackGroupId);

            }
            catch
            {
            }

            return View("MainTrackBooking", model);
        }

        public ActionResult DynamicPlatform()
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
                model.TrackGroupId = 2;
                model.TrackGroup = "T2-Dynamic Platform";
                model.ViewTitle = "T2 Dynamic Platform";

                Session["TrackGroupId"] = model.TrackGroupId;
                Session["TrackGroup"] = model.TrackGroup;
                ViewBag.ServiveLookup = GetServiveLookup();
                ViewBag.TimeSlot = GetTimeSlot();
                ViewBag.ServiveCategory = GetServiveCategory(model.TrackGroupId);

            }
            catch
            {
            }

            return View("MainTrackBooking", model);
        }

        public ActionResult FatigueSurfaceLowSeverity()
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
                model.TrackGroupId = 5;
                model.TrackGroup = "T5a-Fatigue Surface Low Severity";
                model.ViewTitle = "T5a-Fatigue Surface Low Severity";

                Session["TrackGroupId"] = model.TrackGroupId;
                Session["TrackGroup"] = model.TrackGroup;

                ViewBag.TimeSlot = GetTimeSlot();
                ViewBag.ServiveCategory = GetServiveCategory(model.TrackGroupId);

            }
            catch
            {
            }

            return View("MainTrackBooking", model);
        }
        
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
            objdbmlServicesView = new ObservableCollection<dbmlServicesView>(objdbmlServicesView.Where(itm => itm.TrackGroupId == intTrackGroupId));

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
            return Json(new { Status = strStatus, StatusId = intStatusId, ServiceCategoryList = lstCategory, ServicesList = objdbmlServicesView, TrackBookingDetailList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingDetail, TrackBookingTimeDetailList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingTimeDetail }, JsonRequestBehavior.AllowGet);
        }

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
                    foreach (var itm in model)
                    {
                        itm.BookingId = objdbmlBooking.BookingId;
                        itm.BookingDetailId = 0;
                        itm.BPId = 21;
                        itm.Date = objClassUserFunctions.ToDateTimeNotNull(itm.ZZDate);
                        itm.CreateId = Convert.ToInt32(Session["UserId"]);
                        itm.CreateDate = DateTime.Now;
                        itm.UpdateId = Convert.ToInt32(Session["UserId"]);
                        itm.UpdateDate = DateTime.Now;

                        int intRoundOffHrs = Convert.ToInt32(itm.TotalHours);
                        int intRoundOffMin = Convert.ToInt32(itm.TotalMinutes);

                        if((intRoundOffHrs>0 && intRoundOffMin>=30) || (intRoundOffHrs == 0 && intRoundOffMin >= 1))
                        {
                            intRoundOffHrs = intRoundOffHrs + 1;
                            intRoundOffMin=0;
                        }

                        itm.RoundOffHrs = intRoundOffHrs;
                        itm.RoundOffMin = intRoundOffMin;

                        if(intRoundOffHrs>itm.BillingHrs)
                        {
                            itm.BillingHrs = intRoundOffHrs;
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
            return Json(new { Status = strStatus, StatusId = intStatusId, TrackBookingDetailList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingDetail, TrackBookingTimeDetailList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingTimeDetail }, JsonRequestBehavior.AllowGet);
        }

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
                DateTime dtWED = objClassUserFunctions.ToDateTimeNotNull(strWED);

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

        public ActionResult TrackBookingDetailDelete(int intVehicleId, string strDate, int intServiceId, int intTimeSlotId,int intTrackGroupId)
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
            return Json(new { Status = strStatus, StatusId = intStatusId, TrackBookingDetailList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingDetail, TrackBookingTimeDetailList = objreturndbmlTrackBookingDetail.objdbmlTrackBookingTimeDetail }, JsonRequestBehavior.AllowGet);
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
                    ObservableCollection<dbmlProperty> objPropList = new ObservableCollection<dbmlProperty>(objProp.Where(itm => itm.PropertyTypeId == 9));
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
                    returndbmlServicesView objreturndbmlServicesView = objServiceClient.ServicesGetByBPId(12);
                    if (objreturndbmlServicesView.objdbmlStatus.StatusId == 1 && objreturndbmlServicesView.objdbmlServicesView.Count > 0)
                    {
                        Session["Services"] = objreturndbmlServicesView.objdbmlServicesView;
                        objdbmlServicesView = objreturndbmlServicesView.objdbmlServicesView;
                    }
                }

                if (objdbmlServicesView != null && objdbmlServicesView.Count > 0)
                {
                    ObservableCollection<dbmlServicesView> objdbmlServicesViewList = new ObservableCollection<dbmlServicesView>(objdbmlServicesView.Where(itm => itm.TrackGroupId == intTrackGroupId));
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

        public List<SelectListItem> GetServiveLookup()
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
                    returndbmlServicesView objreturndbmlServicesView = objServiceClient.ServicesGetByBPId(12);
                    if (objreturndbmlServicesView.objdbmlStatus.StatusId == 1 && objreturndbmlServicesView.objdbmlServicesView.Count > 0)
                    {
                        Session["Services"] = objreturndbmlServicesView.objdbmlServicesView;
                        objdbmlServicesView = objreturndbmlServicesView.objdbmlServicesView;
                    }
                }

                if (objdbmlServicesView != null && objdbmlServicesView.Count > 0)
                {
                    ObservableCollection<dbmlServicesView> objdbmlServicesViewList = new ObservableCollection<dbmlServicesView>(objdbmlServicesView.Where(itm => itm.TrackGroupId >0));
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



        public ActionResult BrakingTrack()
        {
            return View();
        }

        public ActionResult TestHill()
        {
            return View();
        }

        public ActionResult FatigueSurfaceHighSeverity()
        {
            return View();
        }

        public ActionResult FatigueStructure()
        {
            return View();
        }

        public ActionResult GravelandOffRoad()
        {
            return View();
        }

        public ActionResult HandlingTrackfor4W()
        {
            return View();
        }

        public ActionResult HandlingTrackfor2W()
        {
            return View();
        }

        public ActionResult ComfortTrack()
        {
            return View();
        }

        public ActionResult SustainabilityTrack()
        {
            return View();
        }

        public ActionResult WetSkidPad()
        {
            return View();
        }

        public ActionResult ExternalNoiseTrack()
        {
            return View();
        }


        public ActionResult VillageRoadTrack()
        {
            return View();
        }

        public ActionResult GeneralRoad()
        {
            return View();
        }

        public ActionResult Track()
        {
            return View();
        }

        public ActionResult Lab()
        {
            return View();
        }

        public ActionResult Workshop()
        {
            return View();
        }

        public ActionResult Storage()
        {
            return View();
        }

        public ActionResult AddOnServices()
        {
            return View();
        }              

        public ActionResult TrackDetail()
        {
            return View();
        }

        public ActionResult BookWorkshop()
        {
            return View();
        }

        public ActionResult BookStorage()
        {
            return View();
        }

        public ActionResult CompanyRegistration()
        {
            return View();
        }


        public ActionResult HighSpeedTrackDetail()
        {
            return View();
        }

        public ActionResult DynamicPlatformDetail()
        {
            return View();
        }

        public ActionResult BrakingTrackDetail()
        {
            return View();
        }

        public ActionResult TestHillDetail()
        {
            return View();
        }

        public ActionResult FatigueTrackDetail()
        {
            return View();
        }

        public ActionResult GravelDetail()
        {
            return View();
        }

        public ActionResult HandlingTrack4WDetail()
        {
            return View();
        }

        public ActionResult ComfortTrackDetail()
        {
            return View();
        }

        public ActionResult HandlingTrack2WDetail()
        {
            return View();
        }

        public ActionResult SustainabilityTrackDetail()
        {
            return View();
        }

        public ActionResult WetSkidPadDetail()
        {
            return View();
        }

        public ActionResult VillageRoadDetail()
        {
            return View();
        }

        public ActionResult ExternalNoiseTrackDetail()
        {
            return View();
        }

        public ActionResult GeneralRoadDetail()
        {
            return View();
        }

        public ActionResult InstrumentationLabDetail()
        {
            return View();
        }

        public ActionResult VehicleDyanmicsLabDetail()
        {
            return View();
        }

        public ActionResult PowerTrainLabDetail()
        {
            return View();
        }

        public ActionResult CADCAELabDetail()
        {
            return View();
        }

        public ActionResult ClientWorkshopDetail()
        {
            return View();
        }

        public ActionResult GeneralStorageDetail()
        {
            return View();
        }

        public ActionResult AddOnServicesDetail()
        {
            return View();
        }

        public ActionResult GeneralStorage()
        {
            return View();
        }

        public ActionResult BookPowerTrainLab()
        {
            return View();
        }

        public ActionResult VehicleDynamicsLabVehiclewise()
        {
            return View();
        }

        public ActionResult VehicleDynamicsLabComponentwise()
        {
            return View();
        }

        public ActionResult InstrumentationLabVehiclewise()
        {
            return View();
        }

        public ActionResult InstrumentationLabComponentwise()
        {
            return View();
        }

        public ActionResult InstrumentationLab()
        {
            return View();
        }

        public ActionResult VehicleDynamicsLab()
        {
            return View();
        }

        public ActionResult PowerTrainLab()
        {
            return View();
        }

        public ActionResult CADCAELab()
        {
            return View();
        }

        public ActionResult ElectricalBatteryTestingFacilities()
        {
            return View();
        }

        public ActionResult ElectricalVehicleTestingLab()
        {
            return View();
        }

        public ActionResult KinamaticsComplianceMachineDetail()
        {
            return View();
        }

        public ActionResult SteeringTestRigDetail()
        {
            return View();
        }

        public ActionResult ElastomerTestRigDetail()
        {
            return View();
        }

        public ActionResult DamperTestRigDetail()
        {
            return View();
        }

        public ActionResult ChassisDynoDetail()
        {
            return View();
        }

        public ActionResult EmissionAnalyzersDetail()
        {
            return View();
        }

        public ActionResult WorkstationsDetails()
        {
            return View();
        }

        public ActionResult UnderConstruction()
        {
            return View();
        }




    }
}