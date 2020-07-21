using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PGMSFront.Common;
using PGMSFront.Models;
using PGMSFront.WCFPGMSRef;

namespace PGMSFront.Controllers
{
    public class FrontController : Controller
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
            return View();
        }

        public ActionResult ResetPassword()
        {
            return View();
        }

        #endregion

        #region Company Registration
        public ActionResult CompanyRegistration()
        {
            ViewBag.State = LoadState();
            ViewBag.KnowAbout = LoadKnowAbout();
            return View();
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

        public List<SelectListItem> LoadKnowAbout()
        {
            List<SelectListItem> Items = new List<SelectListItem>();
            try
            {
                returndbmlProperty objreturndbmlProperty = objServiceClient.PropertiesGetByPropertyTypeId(10);
                if (objreturndbmlProperty != null && objreturndbmlProperty.objdbmlStatus.StatusId == 1)
                {
                    foreach (var itm in objreturndbmlProperty.objdbmlProperty)
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

        [ValidateAntiForgeryToken]
        public ActionResult CheckLoginIdAvailability(string strLoginId)
        {
            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlUser objreturndbmlUser = new returndbmlUser();
            try
            {
                objreturndbmlUser = objServiceClient.UserViewGetByLoginIdUserId(strLoginId, 0);
                if (objreturndbmlUser != null && objreturndbmlUser.objdbmlStatus.StatusId == 1 && objreturndbmlUser.objdbmlUserView.Count==0)
                {
                    intStatusId = 1;
                }
                else
                {
                    strStatus = "Login ID "+ strLoginId + " is already taken by some other user";
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        public ActionResult CompanyRegistrationSave(dbmlCompanyView model)
        {
            //if (Session["UserId"] == null)
            //{
            //    return Json(new { Status = "Session Timed Out", StatusId = -99 }, JsonRequestBehavior.AllowGet);
            //}

            int intStatusId = 99;
            string strStatus = "Invalid";

            returndbmlCompanyView objreturndbmlCompanyView = new returndbmlCompanyView();

            try
            {
                model.ServiceBPId = 25;
                model.CreateId = 0;
                model.CreateDate = DateTime.Now;
                model.UpdateId = 0;
                model.UpdateDate = DateTime.Now;
              
                returndbmlCompanyView objreturndbmlCompanyViewTemp = new returndbmlCompanyView();
                ObservableCollection<dbmlCompanyView> objdbmlCompanyViewList = new ObservableCollection<dbmlCompanyView>();
                objdbmlCompanyViewList.Add(model);
                objreturndbmlCompanyViewTemp.objdbmlCompanyView = objdbmlCompanyViewList;

                objreturndbmlCompanyView = objServiceClient.CustomerMasterInsertFront(objreturndbmlCompanyViewTemp);

                if (objreturndbmlCompanyView != null && objreturndbmlCompanyView.objdbmlStatus.StatusId == 1)
                {                    
                    intStatusId = 1;
                    strStatus = "Company registration process for "+ model .CompanyName+ " shall be initiated upon verification of your email-ID.\nVerification link has been sent to '" + model.Email + "'.\nPlease click on link to verify and create password for Login ID - " + model.ZZLoginId + ".";
                }
                else
                {
                    strStatus = objreturndbmlCompanyView.objdbmlStatus.Status;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId }, JsonRequestBehavior.AllowGet);
        }


        #endregion

        #region Others - Track/Lab/Storage etc

        public ActionResult TrackGroupMasterGetAllWithImage(int intTrackGroupId)
        {
            int intStatusId = 99;
            string strStatus = "Invalid";
            ObservableCollection<dbmlTrackGroupMasterWithImageView> objdbmlTrackGroupMasterWithImageView = new ObservableCollection<dbmlTrackGroupMasterWithImageView>();
            try
            {
                if (Session["TrackGroupMasterWithImage"] != null)
                {                  
                    GeneralColl<dbmlTrackGroupMasterWithImageView>.CopyCollection(Session["TrackGroupMasterWithImage"] as ObservableCollection<dbmlTrackGroupMasterWithImageView>, objdbmlTrackGroupMasterWithImageView);
                    if(intTrackGroupId>0)
                    {
                        objdbmlTrackGroupMasterWithImageView = new ObservableCollection<dbmlTrackGroupMasterWithImageView>(objdbmlTrackGroupMasterWithImageView.Where(itm => Convert.ToInt32(itm.TrackGroupId) == intTrackGroupId));
                    }
                }
                else
                {
                    returndbmlTrackGroupMasterWithImageView objreturndbmlTrackGroupMasterWithImageView = objServiceClient.TrackGroupMasterGetAllWithImage();
                    if (objreturndbmlTrackGroupMasterWithImageView != null && objreturndbmlTrackGroupMasterWithImageView.objdbmlStatus.StatusId == 1)
                    {
                        Session["TrackGroupMasterWithImage"] = objreturndbmlTrackGroupMasterWithImageView.objdbmlTrackGroupMasterWithImageView;
                        objdbmlTrackGroupMasterWithImageView = objreturndbmlTrackGroupMasterWithImageView.objdbmlTrackGroupMasterWithImageView;
                        if (intTrackGroupId > 0)
                        {
                            objdbmlTrackGroupMasterWithImageView = new ObservableCollection<dbmlTrackGroupMasterWithImageView>(objdbmlTrackGroupMasterWithImageView.Where(itm => Convert.ToInt32(itm.TrackGroupId) == intTrackGroupId));
                        }                       
                    }
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId,TrackMaster= objdbmlTrackGroupMasterWithImageView }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ServiceDetailViewGetByTrackGroupId(int intTrackGroupId)
        {
            int intStatusId = 99;
            string strStatus = "Invalid";
            returndbmlServiceDetailView objreturndbmlServiceDetailView = new returndbmlServiceDetailView();
            try
            {
                objreturndbmlServiceDetailView = objServiceClient.ServiceDetailViewGetByTrackGroupId(intTrackGroupId);
                if (objreturndbmlServiceDetailView != null && objreturndbmlServiceDetailView.objdbmlStatus.StatusId == 1)
                {
                    intStatusId = 1;
                }
            }
            catch (Exception ex)
            {
                strStatus = ex.Message;
            }
            return Json(new { Status = strStatus, StatusId = intStatusId, TrackDetail = objreturndbmlServiceDetailView }, JsonRequestBehavior.AllowGet);
        }

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

        #endregion
    }
}