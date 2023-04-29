using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using WareHouse22.Models;
using WareHouse22.ViewModel;

namespace WareHouse22.Controllers
{
    public class UsersController : Controller
    {
        private UsersDAL db = new UsersDAL();
        private EquipmentDAL Equipmentdb = new EquipmentDAL();
        private LoansDAL Loansdb = new LoansDAL();

        // GET: Users
        public ActionResult Index()
        {
            return View(db.Users.ToList());
        }
        public ActionResult Home()
        {

            return View("Home");
        }

        public ActionResult UserProfile()
        {
            return View("UserProfile");
        }

        public ActionResult Registration()
        {
            return View("Registration");
        }
        [HttpPost]
        public ActionResult UserRegister(string Email, string FirstName, string LastName, string Password, string Role)
        {
            if (CheckUserExists(Email) == true)
            {
                return Json(new { status = "false" });
            }
            else
            {
                tblUsers u = new tblUsers();
                u.Email = Email;
                u.FirstName = FirstName;
                u.LastName = LastName;
                u.Password = Password;
                u.Role = Role;
                db.Users.Add(u);
                db.SaveChanges();
                return Json(new { status = "true" });
            }
        }
        [HttpPost]
        public ActionResult AddItem(string Code, string Name, string State, string Category, string Instruction, int MaxLoanTime)
        {
            if (CheckItemExists(Code) == false)
            {
                tblEquipment u = new tblEquipment();
                u.Code = Code;
                u.Name = Name;
                u.State = State;
                u.Category = Category;
                u.MaxLoanTime = MaxLoanTime;
                u.Instruction = Instruction;
                Equipmentdb.Equipment.Add(u);
                Equipmentdb.SaveChanges();
                return Json(new { status = "true" });
            }
            else
                return Json(new { status = "false" });
        }

        public Boolean CheckUserExists(string Email)
        {

            foreach (tblUsers u in db.Users)
            {
                if (u.Email == Email)
                {
                    return true;
                }
            }
            return false;
        }

        [HttpPost]
        public ActionResult UserSignIn(string Email, string Password)
        {
            foreach (tblUsers u in db.Users)
            {
                if (u.Email == Email && u.Password == Password)
                {
                    return Json(new { status = "true", name = u.FirstName, role = u.Role });
                }
            }
            return Json(new { status = "false" });
        }
        public Boolean CheckItemExists(string code)
        {
            foreach (tblEquipment u in Equipmentdb.Equipment)
            {
                if (u.Code == code)
                    return true;
            }
            return false;
        }


        [HttpPost]
        public ActionResult ShowEquipment(string SearchInput)
        {
            SearchInput = SearchInput.ToLower();
            EquipmentViewModel f = new EquipmentViewModel();
            List<tblEquipment> equipmentlist = new List<tblEquipment>();
            foreach (tblEquipment u in Equipmentdb.Equipment)
            {
                if (u.Category.ToLower().Equals(SearchInput) || u.Code.ToLower().Contains(SearchInput) || u.Name.ToLower().Contains(SearchInput) || u.State.ToLower().Contains(SearchInput))
                    equipmentlist.Add(u);
            }
            f.ShowEquipments = equipmentlist.ToList();
            return Json(new { equipmentlist });
        }

        [HttpPost]
        public ActionResult LoanItem(string Code, string Email, string StartDate, string ReturnDate)
        {
            int MaxLoanTime = 0;
            if (!CheckLoanAvailableDates(Code, StartDate, ReturnDate))
            {
                return Json(new { status = "false", reason = "taken" });
            }
            if (!CheckLoanAvailableDatesTime(Code, StartDate, ReturnDate, ref MaxLoanTime))
            {
                return Json(new { status = "false", reason = "ToLong", maxloan = MaxLoanTime });
            }
            string name = "";
            foreach (tblEquipment u in Equipmentdb.Equipment)
            {
                if (u.Code == Code)
                    name = u.Name;
            }
            DateTime Start = DateTime.Parse(StartDate);
            DateTime End = DateTime.Parse(ReturnDate);
            tblLoans l = new tblLoans();
            l.ECode = Code;
            l.UserEmail = Email;
            l.ItemName = name;
            l.StartDate = Start;
            l.FinalDate = End;
            Loansdb.Loans.Add(l);
            Loansdb.SaveChanges();
            return Json(new { status = "true" });

        }

        //check if the item is already taken on those dates
        public Boolean CheckLoanAvailableDates(string Code, string StartDate, string ReturnDate)
        {
            DateTime Start = DateTime.Parse(StartDate);
            if (Start < DateTime.Today.AddDays(-1)) { return false; }
            DateTime End = DateTime.Parse(ReturnDate);
            foreach (tblLoans l in Loansdb.Loans)
            {
                if (l.ECode == Code && ((Start >= l.StartDate && Start <= l.FinalDate) || (End >= l.StartDate && End <= l.FinalDate)))
                    return false;
            }
            return true;
        }

        //check if user want to loan for to much time
        public Boolean CheckLoanAvailableDatesTime(string Code, string StartDate, string ReturnDate, ref int MaxLoanTime)
        {
            DateTime Start = DateTime.Parse(StartDate);
            DateTime End = DateTime.Parse(ReturnDate);
            TimeSpan timespan = End - Start;
            int LoanInDays = timespan.Days;
            foreach (tblEquipment u in Equipmentdb.Equipment)
            {
                if (u.Code == Code)
                {
                    if (LoanInDays > u.MaxLoanTime)
                    {

                        MaxLoanTime = (int)u.MaxLoanTime;
                        return false;
                    }
                }
            }
            return true;
        }


        [HttpPost]
        public ActionResult ShowHistory(string userEmail)
        {
            userEmail = userEmail.ToLower();
            LoansViewModel f = new LoansViewModel();
            List<tblLoans> LoanList = new List<tblLoans>();

            foreach (tblLoans u in Loansdb.Loans)
            {

                if (u.UserEmail.ToLower().Equals(userEmail) && u.FinalDate < DateTime.Today)
                {

                    LoanList.Add(u);
                }
            }
            f.ShowLoans = LoanList.ToList();
            return Json(new { LoanList });
        }



        [HttpPost]
        public ActionResult ShowCurrent(string userEmail)
        {
            userEmail = userEmail.ToLower();
            LoansViewModel f = new LoansViewModel();
            List<tblLoans> LoanList = new List<tblLoans>();

            foreach (tblLoans u in Loansdb.Loans)
            {

                if (u.UserEmail.ToLower().Equals(userEmail) && u.StartDate >= DateTime.Today)
                {
                    LoanList.Add(u);
                }
            }
            f.ShowLoans = LoanList.ToList();
            return Json(new { LoanList });
        }

        [HttpPost]
        public ActionResult CancelLoan(string userEmail, string code, string startDate, string finalDate)
        {
            try
            {
                DateTime Start = DateTime.Parse(startDate);
                DateTime End = DateTime.Parse(finalDate);
                foreach (tblLoans u in Loansdb.Loans)
                {

                    if (u.UserEmail.Equals(userEmail) && u.StartDate == Start && u.FinalDate == End && u.ECode.Equals(code))
                    {
                        Loansdb.Loans.Remove(u);
                    }
                }
                        Loansdb.SaveChanges();
                        return Json(new { status = "true" });
            }
            catch
            {
                return Json(new { status = "false" });
            }
        }
        [HttpPost]
        public ActionResult ChangeLoanItem(string Code, string Email, string StartDate, string ReturnDate, string newStartDate, string newReturnDate)
        {
            int MaxLoan = 0;
            foreach (tblEquipment u in Equipmentdb.Equipment)
            {
                if (u.Code.Equals(Code))
                    MaxLoan = (int)u.MaxLoanTime;
            }
            CancelLoan(Email, Code, StartDate, ReturnDate);
            if (CheckLoanAvailableDates(Code, newStartDate, newReturnDate) &&
                CheckLoanAvailableDatesTime(Code, newStartDate, newReturnDate, ref MaxLoan))
            {
                return LoanItem(Code, Email, newStartDate, newReturnDate);
            }
            else
            {
                LoanItem(Code, Email, StartDate, ReturnDate);
                return Json(new { status = "false" });
            }
        }

    }
}


