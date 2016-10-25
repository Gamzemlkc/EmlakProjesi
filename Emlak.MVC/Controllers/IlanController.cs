﻿using Emlak.BLL.Account;
using Emlak.BLL.Repository;
using Emlak.BLL.Settings;
using Emlak.Entity.Entities;
using Emlak.Entity.ViewModels;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace Emlak.MVC.Controllers
{
    public class IlanController : Controller
    {
        // GET: Ilan
        public ActionResult Index()
        {
            return View();
        }
        [Authorize]
        public ActionResult Ekle()
        {
            var userManager = MembershipTools.NewUserManager();
            var user = userManager.FindById(HttpContext.User.Identity.GetUserId());
            if (userManager.IsInRole(user.Id, "Passive") || userManager.IsInRole(user.Id, "Banned"))
            {
                ModelState.AddModelError(string.Empty, "Profiliniz Yeni ilan açmak için uygun değildir.");
                return RedirectToAction("Profile", "Account");
            }
            var model = new KonutViewModel();
            var ilanturleri = new List<SelectListItem>();
            var katturleri = new List<SelectListItem>();
            var isinmaturleri = new List<SelectListItem>();
            new IlanTuruRepo().GetAll().OrderBy(x => x.Ad).ToList().ForEach(x =>
            ilanturleri.Add(new SelectListItem()
            {
                Text = x.Ad,
                Value = x.ID.ToString()
            }));
            new KatTurRepo().GetAll().ForEach(x => katturleri.Add(new SelectListItem
            {
                Text = x.Tur,
                Value = x.ID.ToString()
            }));
            new IsitmaSistemiRepo().GetAll().ForEach(x => isinmaturleri.Add(new SelectListItem
            {
                Text = x.Ad,
                Value = x.ID.ToString()
            }));
            ViewBag.ilanturleri = ilanturleri;
            ViewBag.katturleri = katturleri;
            ViewBag.isinmaturleri = isinmaturleri;

            return View(model);
        }
        [HttpPost, ValidateInput(false)]
        [Authorize]
        public ActionResult Ekle(KonutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Errör");
                return View();
            }
            Konut yeniKonut = new Konut()
            {
                Aciklama = model.Aciklama,
                Adres = model.Adres,
                Baslik = model.Baslik,
                BinaYasi = model.BinaYasi,
                Boylam = model.Boylam,
                Enlem = model.Enlem,
                Fiyat = model.Fiyat,
                IlanTuruID = model.IlanTuruID,
                IsitmaSistemiID = model.IsitmaTuruID,
                KatturID = model.KatTuruID,
                KullaniciID = HttpContext.User.Identity.GetUserId(),
                Metrekare = model.Metrekare,
                OdaSayisi = model.OdaSayisi
            };
            new KonutRepo().Insert(yeniKonut);
            if (model.Dosyalar.Count > 0)
            {
                model.Dosyalar.ForEach(file =>
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                        string extName = Path.GetExtension(file.FileName);
                        fileName = fileName.Replace(" ", "");
                        fileName += Guid.NewGuid().ToString().Replace("-", "");
                        fileName = SiteSettings.UrlFormatConverter(fileName);
                        var klasoryolu = Server.MapPath("~/Upload/" + yeniKonut.ID);
                        var dosyayolu = Server.MapPath("~/Upload/" + yeniKonut.ID + "/") + fileName + extName;
                        if (!Directory.Exists(klasoryolu))
                            Directory.CreateDirectory(klasoryolu);
                        file.SaveAs(dosyayolu);
                        WebImage img = new WebImage(dosyayolu);
                        img.Resize(870, 480, false);
                        img.AddTextWatermark("Wissen", "RoyalBlue", opacity: 75, fontSize: 25, fontFamily: "Verdana");
                        img.Save(dosyayolu);
                        new FotografRepo().Insert(new Fotograf()
                        {
                            KonutID = yeniKonut.ID,
                            Yol = @"Upload/" + yeniKonut.ID + "/" + fileName + extName
                        });
                    }
                });
            }

            return RedirectToAction("Index", "Home");
        }
    }
}