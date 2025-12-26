using System;
using System.Collections.Generic;
using FortuneCookie.Shared;
using Microsoft.EntityFrameworkCore;

namespace FortuneCookie.Server
{
    public static class FortuneBank
    {
        public static void Initialize()
        {
            using (var db = new FortuneDbContext())
            {
                db.Database.EnsureCreated();
                
                 try {
                     using (var command = db.Database.GetDbConnection().CreateCommand())
                     {
                         command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS ""FortuneHistories"" (
                                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_FortuneHistories"" PRIMARY KEY AUTOINCREMENT,
                                ""UserId"" INTEGER NOT NULL,
                                ""FortuneId"" INTEGER NOT NULL,
                                ""ReceivedAt"" TEXT NOT NULL
                            );";
                         db.Database.OpenConnection();
                         command.ExecuteNonQuery();
                     }
                } catch { }

                // Force Reseed for Categories update as per user request
                if (db.Fortunes.Any())
                {
                    db.Fortunes.RemoveRange(db.Fortunes);
                    db.SaveChanges();
                }

                var list = new List<Fortune>
                {
                    // KOMİK (Funny)
                    new Fortune { Text = "Bugün, kahveni bir kez daha dökeceksin ama neyse ki şansın yanındadır.", Category = FortuneCategory.Funny, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Şu an en iyi kararını verdin: Çalışmaya devam etmek yerine bu şans kurabiyesini okumak!", Category = FortuneCategory.Funny, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Bugün hayatında bir değişiklik olacak: Kimse seni aramayacak.", Category = FortuneCategory.Funny, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Şansını denedin ve kazandın. Şimdi biraz daha şanssızlık için hazırlıklı ol!", Category = FortuneCategory.Funny, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Bir gülümseme, ruhunu iyileştirir. Ama yine de işe gitmeyi unutma.", Category = FortuneCategory.Funny, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Bugün karşılaştığın her engeli komik bir şekilde geçeceksin.", Category = FortuneCategory.Funny, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Dünyadaki en güzel şey, kahvaltıyı yatakta yapabilmektir. Bunu yapmadan şansın yok!", Category = FortuneCategory.Funny, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Yavaş ol, belki biraz daha gülersin.", Category = FortuneCategory.Funny, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Şans bugün seninle. Ama saçın sabahki kadar düzgün değil, dikkat et.", Category = FortuneCategory.Funny, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Bugün sana şanslı bir gün dileyebilirim, ama kahve içmeden bunu yapamam.", Category = FortuneCategory.Funny, Rarity = FortuneRarity.Common },

                    // BİLGECE & FELSEFİ & TAVSİYE (Wise)
                    new Fortune { Text = "Başarı, sabır ve azimle gelir; fakat hiçbir zaman aceleye getirilmemelidir.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Senin en büyük gücün, sakinliğini koruyabilmendir.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Hayatta büyük şeyler bekleme; küçük anlar büyük anlamlar taşır.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Rare },
                    new Fortune { Text = "İyi düşün, doğru kararlar ver, ve hayatının kontrolünü elinde tut.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Gerçek mutluluk, sahip olduklarımızı kabul etmekle başlar.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Bugün, tüm potansiyelini serbest bırakmak için harika bir gün.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Her gün bir fırsat, her an bir öğretmendir.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Kötü günler, iyi günlerin değerini anlamanı sağlar.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Senin gerçek gücün, karşılaştığın zorluklarla nasıl başa çıktığındır.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Hayatın en güzel anları, en basit olanlardır.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Legendary },
                    new Fortune { Text = "Hayat bir yolculuktur, her anın tadını çıkar.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Huzuru bulmak için içsel dinginliğe ihtiyacın var.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Rare },
                    new Fortune { Text = "İnsanlar, düşüncelerine göre şekillenir; bu yüzden iyi düşün.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Her şey geçici, o yüzden anı yaşa.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Zihnindeki düşünceler, gerçekliğini yaratır.", Category = FortuneCategory.Wise, Rarity = FortuneRarity.Common },

                    // MOTİVASYONEL & KİŞİSEL GELİŞİM (Motivational)
                    new Fortune { Text = "Hedeflerine ulaşmak için en önemli adım, ilk adımı atmaktır.", Category = FortuneCategory.Motivational, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Yılma! En karanlık anlarda bile ışık bulabilirsin.", Category = FortuneCategory.Motivational, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Büyük başarılar, küçük adımlarla başlar.", Category = FortuneCategory.Motivational, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Şimdiye kadar ne kadar zorlandığını düşünme, gelecekte ne kadar güçlü olacağına odaklan.", Category = FortuneCategory.Motivational, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Kendi potansiyelini keşfetmek için bir sınır yok.", Category = FortuneCategory.Motivational, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Bugün bir adım daha at, yarın seni daha yakın bir noktada bulacaksın.", Category = FortuneCategory.Motivational, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Kendini keşfetmek, en değerli yolculuklardan biridir.", Category = FortuneCategory.Motivational, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Kendi sınırlarını zorla, çünkü büyüme orada başlar.", Category = FortuneCategory.Motivational, Rarity = FortuneRarity.Rare },
                    new Fortune { Text = "Her küçük başarın, seni büyük hedeflerine bir adım daha yaklaştıracak.", Category = FortuneCategory.Motivational, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Kendine inan; başaramayacağın şey yok.", Category = FortuneCategory.Motivational, Rarity = FortuneRarity.Legendary },

                    // ROMANTİK (Romantic)
                    new Fortune { Text = "Bugün kalbinin peşinden git, sevgiyi bulacaksın.", Category = FortuneCategory.Romantic, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Aşk, karşılaştığın bir kişiden çok, hissettiğin bir duygudur.", Category = FortuneCategory.Romantic, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Kalbini dinle, seni nereye götüreceğini bileceksin.", Category = FortuneCategory.Romantic, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Birinin seni düşünmesi, hayatındaki en güzel anlardan biridir.", Category = FortuneCategory.Romantic, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Aşkını bulduğunda, dünya daha parlak görünecek.", Category = FortuneCategory.Romantic, Rarity = FortuneRarity.Rare },
                    new Fortune { Text = "Sevgi, hayatı anlamlı kılan en güçlü bağdır.", Category = FortuneCategory.Romantic, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Bugün sevdiklerinle geçireceğin anlar, kalbinde iz bırakacak.", Category = FortuneCategory.Romantic, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Aşk, sadece sözlerde değil, hareketlerde de hissedilir.", Category = FortuneCategory.Romantic, Rarity = FortuneRarity.Common },

                    // GENEL & İLGİNÇ & ŞANSLI SAYILAR (General)
                    new Fortune { Text = "Bugün şanslı sayın: 7. Bu sayıyı hayatındaki önemli kararlarda kullan!", Category = FortuneCategory.General, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Şanslı günün pazar. Hayatındaki fırsatları değerlendirebilirsin.", Category = FortuneCategory.General, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Şanslı renklerin: mavi ve yeşil. Bu renkleri etrafında bulundur.", Category = FortuneCategory.General, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Bugün bir sürpriz seni bekliyor. Hazırlıklı ol!", Category = FortuneCategory.General, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "En büyük keşif, kendi iç yolculuğundadır.", Category = FortuneCategory.General, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Bugün bir deney yap; asla kaybetmezsin.", Category = FortuneCategory.General, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Eğer dikkatlice bakarsan, evrende sana mesajlar gönderen bir şeyler bulacaksın.", Category = FortuneCategory.General, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Hayat bir bilmecedir; çözümü içindesin.", Category = FortuneCategory.General, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Her sorunun cevabı, senin içinde gizli.", Category = FortuneCategory.General, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Şans, sadece bir yansıma değildir; onu yaratmak senin elinde.", Category = FortuneCategory.General, Rarity = FortuneRarity.Common },

                    // LANETLİ (Keeping exiting ones as per user context implies keeping specific categories separate)
                    new Fortune { Text = "Arkanı kollasan iyi olur...", Category = FortuneCategory.Cursed, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Bugün merdiven altından geçme.", Category = FortuneCategory.Cursed, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Kara kedi seni izliyor.", Category = FortuneCategory.Cursed, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Telefonun şarjı en kritik anda bitecek.", Category = FortuneCategory.Cursed, Rarity = FortuneRarity.Common },
                    new Fortune { Text = "Çorabın ıslak zemine basacak.", Category = FortuneCategory.Cursed, Rarity = FortuneRarity.Common }
                };
                
                db.Fortunes.AddRange(list);
                db.SaveChanges();
                Console.WriteLine("Database re-seeded with 50 categorized fortunes.");
            }
        }

        public static Fortune GetRandomFortune(FortuneCategory? filter = null)
        {
            using var db = new FortuneDbContext();
            
            IQueryable<Fortune> query = db.Fortunes;
            if (filter.HasValue)
            {
                query = query.Where(f => f.Category == filter.Value);
            }

            int count = query.Count();
            if (count == 0) 
            {
                 // Fallback if no fortune in category, try General or just return Cursed
                 if (filter.HasValue && filter.Value != FortuneCategory.General) return GetRandomFortune(FortuneCategory.General);
                 return new Fortune { Text = "Bu kategoride fal bulunamadı!", Category = FortuneCategory.Cursed };
            }
            
            int index = new Random().Next(count);
            var fortune = System.Linq.Enumerable.First(System.Linq.Queryable.Skip(query, index), f => true);
            
            // Generate Lucky Numbers (6 random numbers 1-49)
            var rnd = new Random();
            fortune.LuckyNumbers = new int[6];
            for(int i=0; i<6; i++) fortune.LuckyNumbers[i] = rnd.Next(1, 50);
            Array.Sort(fortune.LuckyNumbers);
            
            return fortune;
        }

        public static void AddFortune(string text, FortuneCategory category, int? userId = null)
        {
            using var db = new FortuneDbContext();
            db.Fortunes.Add(new Fortune { Text = text, Category = category, Rarity = FortuneRarity.Common, AddedByUserId = userId });
            db.SaveChanges();
        }

        public static List<Fortune> GetFortunesByUser(int userId)
        {
            using var db = new FortuneDbContext();
            return System.Linq.Enumerable.ToList(db.Fortunes.Where(f => f.AddedByUserId == userId).OrderByDescending(f => f.Id));
        }
    }
}
