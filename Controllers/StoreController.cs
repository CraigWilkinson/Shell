﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Shell.DAL;
using Shell.Models;
using Shell.ViewModels;
using Shell.ViewModels.Product;

namespace Shell.Controllers
{
    public class StoreController : Controller
    {
        StoreRepository storeRepo = new StoreRepository();
        S3Client S3Client = new S3Client();

        // GET: Store
        public ActionResult Index()
        {
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            var categories = storeRepo.CategoriesWithMostProducts();
            var latestListings = storeRepo.LatestListings();
            foreach (var l in latestListings)
            {
                Debug.WriteLine(string.Format("Title: {0} URL: {1}", l.Title, l.Images.First().URL));
            }

            return View(new StoreFrontViewModel
            {
                Categories = categories,
                LatestListings = latestListings
            });
        }

        [Authorize]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public ActionResult Create([Bind(Include = "Title,Description,Price,Category,File")] CreateProductViewModel model)
        {
            string ImageURL;
            if (model.File != null)
            {
                ImageURL = S3Client.PutImage(model.File);
            }
            else
            {
                ImageURL = "dummy";
            }
            Product product = new Product
            {
                Title = model.Title,
                Description = model.Description,
                Price = model.Price,
                Category = new Category
                {
                    Name = model.Category
                },
                DateListed = DateTime.Now,
                Images = new List<ProductImage>
                {
                    new ProductImage
                    {
                        URL = ImageURL
                    }
                }
            };

            var result = storeRepo.AddNewProduct(product);

            if (result)
            {
                return RedirectToAction("Index");
            }

            return View("Error");
        }

        public ActionResult Browse()
        {
            return View("Browse", storeRepo.AllProducts().Take(10));
        }

        [HttpPost]
        public ActionResult Browse(string searchString)
        {
            var model = storeRepo.AllProducts(searchString).Take(10);
            Debug.WriteLine("Here");
            if (Request.IsAjaxRequest())
            {
                Debug.WriteLine("IsAjax");
                return PartialView("_Listings", model);
            }

            return View("Browse", model);
        }

        public ActionResult Details()
        {
            return View();
        }
    }
}