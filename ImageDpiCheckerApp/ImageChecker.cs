﻿using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDpiCheckerApp
{
    public partial class ImageChecker
    {
        public static List<Tuple<string, string, bool>> storedFiles = new List<Tuple<string, string, bool>>();

        public static string targetDirectory;


        public static List<Tuple<string, string, bool>> GetFilteredFiles(string directoryToScan, string[] filters)
        {
            targetDirectory = directoryToScan;

            var filteredFiles = GetFilesFrom(targetDirectory, filters);

            return LoopThroughFiles(filteredFiles);
        }


        public static List<Tuple<string, string, bool>> LoopThroughFiles(string[] filteredFiles)
        {
            bool hasException = false;
            foreach (var fileLoc in filteredFiles)
            {
                try
                {
                    Tuple<string, string, bool> currentImage = null;
                    string filename = Path.GetFileName(fileLoc);
                    currentImage = filename.Contains(".pdf") ? GetDPIFromPdf(fileLoc) : GetDPIFromImage(fileLoc);
                    storedFiles.Add(currentImage);
                }
                catch (Exception e)
                {
                    hasException = true;
                }
            }
            if (hasException)
                Console.WriteLine("An unexpected error has occurred and one or more DPI's could not be read");
            return storedFiles;
        }

        public static String[] GetFilesFrom(String searchFolder, String[] filters)
        {
            List<String> filesFound = new List<String>();
            try
            {

                foreach (var filter in filters)
                {
                    filesFound.AddRange(Directory.GetFiles(searchFolder, String.Format("*.{0}", filter), SearchOption.AllDirectories));
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("The given file path could not be found");
            }
            return filesFound.ToArray();
        }

        public static Tuple<string, string, bool> GetDPIFromImage(string fileLoc)
        {
            string filename = FormatFileName(fileLoc);

            return Tuple.Create(filename, Bitmap.FromFile(fileLoc).HorizontalResolution.ToString(), true);
        }

        public static Tuple<string, string, bool> GetDPIFromPdf(string fileLoc)
        {
            bool isPicture = false;
            string filename = FormatFileName(fileLoc);
            string dpiOfPDF = "---";
            using (var reader = new PdfReader(fileLoc))
            {
                PdfDictionary pg = reader.GetPageN(1);
                PdfDictionary res = (PdfDictionary)PdfReader.GetPdfObject(pg.Get(PdfName.RESOURCES));
                PdfDictionary xobjs = (PdfDictionary)PdfReader.GetPdfObject(res.Get(PdfName.XOBJECT));
                iTextSharp.text.Rectangle cropbox = reader.GetCropBox(1);
                var pageWidthInInch = cropbox.Width / 72;
                if (xobjs != null)
                {
                    foreach (PdfName xObjectKey in xobjs.Keys)
                    {
                        PdfObject xobj = xobjs.Get(xObjectKey);
                        PdfDictionary tg = (PdfDictionary)PdfReader.GetPdfObject(xobj);
                        PdfName subtype = (PdfName)PdfReader.GetPdfObject(tg.Get(PdfName.SUBTYPE));
                        if (subtype.Equals(PdfName.IMAGE))
                        {
                            PdfNumber width = (PdfNumber)tg.Get(PdfName.WIDTH);
                            PdfNumber height = (PdfNumber)tg.Get(PdfName.HEIGHT);
                            dpiOfPDF = Convert.ToString(width.FloatValue / pageWidthInInch);
                            isPicture = true;
                        }
                    }
                }
            }
            return Tuple.Create(filename, dpiOfPDF, isPicture);
        }

        public static string FormatFileName(string fileLoc)
        {
            string filename = fileLoc.Replace(targetDirectory, "");
            if (filename.Substring(0, 1) == @"\")
                return filename = filename.Substring(1);
            return filename;
        }

    }
}
