using Microsoft.EntityFrameworkCore;
using DoAnTotNghiep.Models;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;

namespace DoAnTotNghiep.Data
{
    public static class SeedData
    {
        public static void Initialize(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                //Seed Categories
                if (!context.Categories.Any())
                {
                    logger.LogInformation("Seeding categories...");
                    context.Categories.AddRange(
                        new Category { Name = "Bồn cầu" },
                        new Category { Name = "Vòi sen" },
                        new Category { Name = "Lavabo" },
                        new Category { Name = "Vòi Lavabo" } 
                    );
                    context.SaveChanges();
                    logger.LogInformation("Categories seeded successfully.");
                }

                // Seed Products 
                if (!context.Products.Any())
                {
                    logger.LogInformation("Seeding products...");
                    // Lấy ID 
                    var bonCauCategory = context.Categories.FirstOrDefault(c => c.Name == "Bồn cầu");
                    var voiSenCategory = context.Categories.FirstOrDefault(c => c.Name == "Vòi sen");
                    var lavaboCategory = context.Categories.FirstOrDefault(c => c.Name == "Lavabo");
                    var voiLavaboCategory = context.Categories.FirstOrDefault(c => c.Name == "Vòi Lavabo");


                    if (bonCauCategory != null && voiSenCategory != null && lavaboCategory != null && voiLavaboCategory != null)
                    {
                        context.Products.AddRange(
                            new Product
                            {
                                Name = "Bàn cầu điện tử kèm remote điều khiển MOEN - Walden - HKSW1291C",
                                Price = 8000000M,
                                ImageUrl = "/images/product-1.jpg",
                                CategoryId = bonCauCategory.Id
                            },
                            new Product
                            {
                                Name = "Bộ sen tắm nóng lạnh màu SRN - 63332SRN + 2297SRN +M22072SL",
                                Price = 29000000M,
                                ImageUrl = "/images/product-2.jpg",
                                CategoryId = voiSenCategory.Id
                            },
                            new Product
                            {
                                Name = "Lavabo đặt bàn Galassia - DREAM - 7300OC",
                                Price = 26820000M,
                                ImageUrl = "/images/product-3.jpg",
                                CategoryId = lavaboCategory.Id
                            },
                            new Product 
                            {
                                Name = "Vòi lavabo nóng lạnh kèm xả nhấn Fiore Rubinetterie - KUBE Chrome Black - 100CN8515",
                                Price = 1800000M,
                                ImageUrl = "/images/product-4.jpg",
                                CategoryId = voiLavaboCategory.Id
                            }
                        );
                        context.SaveChanges();
                        logger.LogInformation("Products seeded successfully.");
                    }
                    else {
                        logger.LogWarning("Could not find all categories to seed products.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the data.");
            }
        }
    }
}

