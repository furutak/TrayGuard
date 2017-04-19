using System;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Data;
using System.Text;
using System.Runtime.InteropServices;

namespace TrayGuard
{
    public class TfSato
    {
        // プリント名を、基本設定ファイルから取得するための変数
        string appconfig = System.Environment.CurrentDirectory + "\\info.ini";
        

        // プリントコマンド用変数
        IntPtr mFont;
        IntPtr mFontOld;
        string trayPackCartonId;
        string trayIdQtyCombo;
        DataTable dtLblContent;
        DataTable dtLotSumary;
        string vendor;
        string pn;
        string apn;
        string rev;
        string config;
        string desc;
        string datecode;
        string qty;
        string footnote;
        string packdate;
        string model;
        string ltype2; // Tray, Pack, Carton
        string ltype3; // Foxcon = "Fox", Pegatron = "Pega"
        string printerName;
        int page_counter;
        int data_counter;
        int y;
        int a = 8; // 上部文字始点高さから、下部文字始点高さまでの幅（ａ＝８、文字高さ＝５の場合は、文字間の幅＝３）
        int b = 6; // 上部文字始点高さから、下部バーコード始点高さまでの幅（ａ＝６、文字高さ＝５の場合は、文字とバーコードの幅＝１）

        // コンストラクタ
        public TfSato()
        {
            // プリンター名を、設定ファイルから取得する
            //printerName = "SATO CZ408";
            printerName = readIni("PRINTER NAME", "PRINTER", appconfig);
        }

        // 印刷ボタン押下時処理
        public void printStart(string labelType1, string id, DataTable dtLot, string batch, DateTime pdate, 
            string labelType2, string labelType3, short copies, string bin)
        {
            ltype2 = labelType2;
            ltype3 = labelType3;
            trayPackCartonId = id;
            dtLotSumary = dtLot;
            qty = dtLotSumary.Rows.Count >= 0 ? dtLotSumary.Rows[0]["total"].ToString() : string.Empty;
            trayIdQtyCombo = id + " " + qty.ToString();

            // ロットサマリーテーブルの格納、および、ＤＢ上ラベルコンテンツ情報の取得
            if (labelType1 != "tray")
            {
                rev = batch;
                packdate = pdate.ToString("yyyy/M/d");

                dtLblContent = new DataTable();
                string sql = "select model, header, content from t_label_content";
                TfSQL tf = new TfSQL();
                tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql, ref dtLblContent);

                vendor = pickUpFromDataTable(dtLblContent, "VENDOR");
                config = pickUpFromDataTable(dtLblContent, "CONFIG");
                desc = pickUpFromDataTable(dtLblContent, "DESC");
                footnote = pickUpFromDataTable(dtLblContent, "FOOTNOTE");
                model = pickUpFromDataTable(dtLblContent, "MODEL");

                string pnFox = pickUpFromDataTable(dtLblContent, "P/N_FOX");
                string pnPega = pickUpFromDataTable(dtLblContent, "P/N_PEGA");
                apn = pnFox;
                pn = (ltype3 == "Fox") ? apn : (ltype3 == "Pega") ? pnPega : (ltype3 == "Non") ? "Non" : "Error!";
            }

            // PrintDocumentコンポーネントオブジェクトを生成
            System.Drawing.Printing.PrintDocument pd = new System.Drawing.Printing.PrintDocument(); 
            pd.PrinterSettings.PrinterName = printerName;    // 出力先プリンタ名を指定
            pd.PrinterSettings.Copies = copies; // 印字枚数を指定

            // PrintPageイベントハンドラに追加
            if (labelType1 == "tray")
                pd.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(printTray);
            else if (labelType1 == "packCartonInternal") {
                // 2016.07.28 PEC FUJIKI (印刷部分の仕様変更対応で追加) FROM
                page_counter = 0;
                data_counter = 0;
                // 2016.07.28 PEC FUJIKI (印刷部分の仕様変更対応で追加) TO
                pd.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(printPackCartonPalletInternal);
            }
            else if (labelType1 == "packCartonPega")
                pd.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(printPackCartonPalletPega);

            pd.Print();   // 印刷の選択ダイアログを表示
        }

        // サブプロシージャ：ＤＢ上ラベルコンテンツ情報の取得
        private string pickUpFromDataTable(DataTable dt, string content)
        {
            DataView dv = new DataView(dt);
            dv.RowFilter = "header = '" + content + "'";
            return dv[0]["content"].ToString();
        }

        // ラベル発行処理（トレー）
        private void printTray(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Pen blackPen = new Pen(Color.Black, 1);  // Penオブジェクトを作成(幅1の黒色)
            try
            {
                // サトープリンタドライバにフォントを使用（事前にサトープリンタドライバにフォントの追加作業が必要）
                int intX = 0;    // Ｘ座標
                int intY = 0;    // Ｙ座標
                int intFont = 0; // フォントサイズ
                IntPtr hdc = e.Graphics.GetHdc(); // デバイスコンテキストを識別するハンドルを取得

                // データマトリクスのプリント
                intX = 25;
                intY = 2;
                intFont = 40;  // この設定は、データマトリクスには影響なし。ドライバのフォントで設定を行う。
                SetFont(hdc, "DATAMATRIX", intFont);
                PrintTextOut(hdc, intX, intY, trayPackCartonId, trayPackCartonId.Length);

                // 通常文字列のプリント
                intX = 31;
                intY = 2;
                intFont = 30;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, trayIdQtyCombo, trayIdQtyCombo.Length);

                // 後処理
                DeleteObject(mFont);          // グラフィックオブジェクトを削除し、システムリソースの解放
                SelectObject(hdc, mFontOld);  // デバイスコンテキストにオブジェクトの選択
                e.Graphics.ReleaseHdc(hdc);   // デバイスコンテキストハンドルを解放
            }
            catch { MessageBox.Show("Label printing failed.", "Print Result", MessageBoxButtons.OK); }
            finally { blackPen.Dispose(); }
        }

        // ラベル発行処理（パック・カートン・パレット、社内）
        private void printPackCartonPalletInternal(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            // ********************************************************************************
            // 2016.07.28 PEC FUJIKI (印刷部分の仕様変更対応で本関数内全面的に改修)
            // ********************************************************************************

            // 描画・文字、共通変数
            a = 7; // 上下文字始点Ｙ座標間の距離（ａ＝８、文字高さ＝５の場合は、文字間の幅＝３）
            b = 6; // その他、調整用幅
            Pen blackPen = new Pen(Color.Black, 1);    // Penオブジェクトを作成(幅1の黒色)        
            e.Graphics.PageUnit = GraphicsUnit.Millimeter;  // Graphicsの設定をします

            int row_position = 3;
            int row_max = 80;

            try
            {
                if (ltype2 != "Pallet")
                {
                    Single dx1;    // 枠線描画用Ｘ座標１
                    Single dy1;    // 枠線描画用Ｙ座標１
                    Single dx2;    // 枠線描画用Ｘ座標２
                    Single dy2;    // 枠線描画用Ｙ座標２

                    int line_y1 = row_position + (page_counter == 0 ? (a * 6) : a);
                    int line_y2 = line_y1;

                    // for (int i = data_counter; i <= dtLotSumary.Columns.Count; i+=2)
                    // 2017.01.17 PEC FUJII (最終行はＯＴＨＥＲとＴＯＴＡＬを印刷するため、ループを１回増)
                    for (int i = data_counter; i <= dtLotSumary.Columns.Count + 2; i+=2)
                    {
                        //線描画：横棒 ×（ロット件数／２＋１）の繰り返し
                        dx1 = 10; dy1 = line_y2; dx2 = 90; dy2 = line_y2;
                        e.Graphics.DrawLine(blackPen, dx1, dy1, dx2, dy2);
                        if (line_y2 > row_max) break;
                        line_y2 += a;
                    }
                    // 線描画：横棒 
                    dx1 = 10; dy1 = line_y2; dx2 = 90; dy2 = line_y2;
                    e.Graphics.DrawLine(blackPen, dx1, dy1, dx2, dy2);
                    // 枠線描画：５本の縦線、ロット件数に応じた長さ、その１
                    dx1 = 10; dy1 = line_y1; dx2 = 10; dy2 = line_y2;
                    e.Graphics.DrawLine(blackPen, dx1, dy1, dx2, dy2);
                    // 枠線描画：５本の縦線、ロット件数に応じた長さ、その２
                    dx1 = 36; dy1 = line_y1; dx2 = 36; dy2 = line_y2;
                    e.Graphics.DrawLine(blackPen, dx1, dy1, dx2, dy2);
                    // 枠線描画：５本の縦線、ロット件数に応じた長さ、その３
                    dx1 = 50; dy1 = line_y1; dx2 = 50; dy2 = line_y2;
                    e.Graphics.DrawLine(blackPen, dx1, dy1, dx2, dy2);
                    // 枠線描画：５本の縦線、ロット件数に応じた長さ、その４
                    dx1 = 76; dy1 = line_y1; dx2 = 76; dy2 = line_y2;
                    e.Graphics.DrawLine(blackPen, dx1, dy1, dx2, dy2);
                    // 枠線描画：５本の縦線、ロット件数に応じた長さ、その５
                    dx1 = 90; dy1 = line_y1; dx2 = 90; dy2 = line_y2;
                    e.Graphics.DrawLine(blackPen, dx1, dy1, dx2, dy2);
                }

                // サトープリンタドライバにフォントを使用（事前にサトープリンタドライバにフォントの追加作業が必要）
                int intX = 0;    // Ｘ座標
                int intY = 0;    // Ｙ座標
                int intFont = 0; // フォントサイズ
                IntPtr hdc = e.Graphics.GetHdc(); // デバイスコンテキストを識別するハンドルを取得

                // ID表示
                if (ltype2 != "")
                {
                    // 見出文字：パックＩＤ
                    string idTypeString = (ltype2 == "Pack") ? "Pack ID:" : (ltype2 == "Carton") ? "Carton ID:" : (ltype2 == "Pallet") ? "Pallet ID:" : "ERROR!";
                    intX = 10; intY = row_position; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, idTypeString, idTypeString.Length);
                    // 本文変数：パックＩＤ
                    intX = 35; intY = row_position; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, trayPackCartonId, trayPackCartonId.Length);
                    // 本文バー：パックＩＤ
                    intX = 75; intY = row_position; intFont = 40;
                    SetFont(hdc, "DATAMATRIX", intFont);
                    PrintTextOut(hdc, intX, intY, trayPackCartonId, trayPackCartonId.Length);
                    // 本文変数：パックＩＤ
                    row_position += a;
                }

                // 文字表示
                if (ltype2 != "" && page_counter == 0)
                {
                    // 見出文字：ＶＥＮＤＯＲ
                    intX = 10; intY = row_position; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, "Vendor:", "Vendor:".Length);
                    // 本文変数：ＶＥＮＤＯＲ
                    intX = 35; intY = row_position; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, vendor, vendor.Length);
                    row_position += a;
                    // 見出文字：産地
                    intX = 10; intY = row_position; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, "Origin:", "Origin:".Length);
                    // 本文変数：産地
                    intX = 35; intY = row_position; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, "CN", "CN".Length);
                    row_position += a;
                    // 見出文字：梱包日
                    intX = 10; intY = row_position; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, "Pack Date:", "Pack Date:".Length);
                    // 本文変数：梱包日
                    intX = 35; intY = row_position; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, packdate, packdate.Length);
                    row_position += a;
                    // 見出文字：一般Ｐ／Ｎ
                    intX = 10; intY = row_position; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, "P/N:", "P/N:".Length);
                    if (!pn.Equals("Non"))
                    {
                        // 本文変数：一般Ｐ／Ｎ（ＦＯＸＣＯＮ ＯＲ ＰＥＧＡＴＲＯＮ で場合分け）
                        intX = 35; intY = row_position; intFont = 40;
                        SetFont(hdc, "XM", intFont);
                        PrintTextOut(hdc, intX, intY, pn, pn.Length);
                    }
                    row_position += a;
                    // 見出文字：ＭＯＤＥＬ
                    intX = 10; intY = row_position; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, "Vndr P/N:", "Vndr P/N:".Length);
                    // 本文変数：ＭＯＤＥＬ
                    intX = 35; intY = row_position; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, model, model.Length);
                    row_position += a;
                }
                // 集計行
                if (ltype2 == "Pallet" && page_counter == 0)
                {
                    // 見出文字：ＱＵＡＮＴＩＴＹ（ＴＯＴＡＬ）
                    intX = 10; intY = row_position; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, "Quantity:", "Quantity:".Length);
                    // 本文変数：ＱＵＡＮＴＩＴＹ（ＴＯＴＡＬ）
                    intX = 35; intY = row_position; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, qty, qty.Length);
                    row_position += a;
                }
                // 明細ヘッダ
                if (ltype2 != "Pallet")
                {
                    // 枠線内ヘッダー文字：ＤＡＴＥ ＣＯＤＥ
                    intX = 12; intY = row_position + 1; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, "Date CD", "Date CD".Length);
                    // 枠線内ヘッダー文字：ＱＴＹ
                    intX = 38; intY = row_position + 1; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, "Qty", "Qty".Length);
                    // 枠線内ヘッダー文字：ＤＡＴＥ ＣＯＤＥ
                    intX = 52; intY = row_position + 1; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, "Date CD", "Date CD".Length);
                    // 枠線内ヘッダー文字：ＱＴＹ
                    intX = 78; intY = row_position + 1; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, "Qty", "Qty".Length);
                    row_position += a;
                }

                // 明細  // 2017.01.17 PEC FUJII (１ページで印刷を完了、最終行はＯＴＨＥＲとＴＯＴＡＬ表示、へ変更) FROM
                if (ltype2 != "Pallet")
                {
                    int qtySubTotal = 0;
                    int lotCount = dtLotSumary.Columns.Count - 1;
                    int lotPrintFinal = lotCount <= 8 ? lotCount : 8;
                    for (int i = 0; i < lotPrintFinal; i++)
                    {
                        datecode = dtLotSumary.Columns[i].ColumnName.ToString();
                        qty = dtLotSumary.Rows[0][i].ToString();
                        int x1 = (i % 2 == 0) ? 12 : 52;
                        int x2 = (i % 2 == 0) ? 38 : 78;
                        // 本文変数：ＤＡＴＥ ＣＯＤＥ
                        intX = x1; intY = row_position + 1; intFont = 40;
                        SetFont(hdc, "XM", intFont);
                        PrintTextOut(hdc, intX, intY, datecode, datecode.Length);
                        // 本文変数：ＱＴＹ
                        intX = x2; intY = row_position + 1; intFont = 40;
                        SetFont(hdc, "XM", intFont);
                        PrintTextOut(hdc, intX, intY, qty, qty.Length);

                        qtySubTotal += (int)dtLotSumary.Rows[0][i];
                        row_position = ((i % 2 == 1) || i == lotPrintFinal - 1) ? row_position + a : row_position;
                    }

                    int total = dtLotSumary.Rows.Count >= 0 ? (int)dtLotSumary.Rows[0]["total"] : 0;
                    int other = total - qtySubTotal;

                    // １枚のラベルに印刷できない端数ロットが存在する場合のみ、ＯＴＨＥＲ ＬＯＴＳとして表示する
                    if (lotCount != lotPrintFinal)
                    {
                        // 本文変数：ＯＴＨＥＲ
                        intX = 12; intY = row_position + 1; intFont = 40;
                        SetFont(hdc, "XM", intFont);
                        PrintTextOut(hdc, intX, intY, "OTHER LOTS", "OTHER LOTS".Length);
                        // 本文変数：ＱＴＹ（ＯＴＨＥＲ）
                        intX = 38; intY = row_position + 1; intFont = 40;
                        SetFont(hdc, "XM", intFont);
                        PrintTextOut(hdc, intX, intY, other.ToString(), other.ToString().Length);
                    }

                    // 本文変数：ＴＯＴＡＬ
                    intX = 52; intY = row_position + 1; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, "TOTAL", "TOTAL".Length);
                    // 本文変数：ＱＴＹ（ＴＯＴＡＬ）
                    intX = 78; intY = row_position + 1; intFont = 40;
                    SetFont(hdc, "XM", intFont);
                    PrintTextOut(hdc, intX, intY, total.ToString(), total.ToString().Length);
                }
                // 2017.01.17 PEC FUJII (１ページで印刷を完了、最終行はＯＴＨＥＲとＴＯＴＡＬ表示、へ変更) TO

                //// 明細
                //if (ltype2 != "Pallet")
                //{
                //    for (int i = data_counter; i < dtLotSumary.Columns.Count - 1; i++)
                //    {
                //        datecode = dtLotSumary.Columns[i].ColumnName.ToString();
                //        qty = dtLotSumary.Rows[0][i].ToString();
                //        int x1 = (i % 2 == 0) ? 12 : 52;
                //        int x2 = (i % 2 == 0) ? 38 : 78;
                //        // 本文変数：ＤＡＴＥ ＣＯＤＥ
                //        intX = x1; intY = row_position + 1; intFont = 40;
                //        SetFont(hdc, "XM", intFont);
                //        PrintTextOut(hdc, intX, intY, datecode, datecode.Length);
                //        // 本文変数：ＱＴＹ
                //        intX = x2; intY = row_position + 1; intFont = 40;
                //        SetFont(hdc, "XM", intFont);
                //        PrintTextOut(hdc, intX, intY, qty, qty.Length);
                //        data_counter++;

                //        row_position = (i % 2 == 1) ? row_position + a : row_position;
                //        if (row_position > row_max) break;
                //    }
                //}
                page_counter++;
                e.Graphics.ReleaseHdc(hdc);   // デバイスコンテキストハンドルを解放
                SelectObject(hdc, mFontOld);  // デバイスコンテキストにオブジェクトの選択
                if (data_counter < dtLotSumary.Columns.Count - 1 && ltype2 != "Pallet")
                {
                    // e.HasMorePages = true;
                    // 2017.01.17 PEC FUJII (１ページで印刷を完了、最終行はＯＴＨＥＲとＴＯＴＡＬ表示、へ変更)
                    e.HasMorePages = false;
                }
                else
                {
                    e.HasMorePages = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Print failed." + System.Environment.NewLine + ex.Message,
                    "Print Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                blackPen.Dispose();
                DeleteObject(mFont);          // グラフィックオブジェクトを削除し、システムリソースの解放
            }



            /*

                        // パレットの場合は、ロットごとの数量明細は不要のため、枠線を印字しない
                        if (ltype2 != "Pallet")
                        {
                            // 描画はＴＲＹ節に組み込むことができないため、ＴＲＹ節の外て処理
                            Single dx1;    // 枠線描画用Ｘ座標１
                            Single dy1;    // 枠線描画用Ｙ座標１
                            Single dx2;    // 枠線描画用Ｘ座標２
                            Single dy2;    // 枠線描画用Ｙ座標２
                            int c;         // 枠線高さ、一時保持
                            int d;         // 枠線高さ、一時保持

                            // 高さの保持
                            c = 45; d = 45;
                            // 枠線描画：横棒 ×（ロット件数／２＋１）の繰り返し
                            for (int i = 0; i <= dtLotSumary.Columns.Count / 2 + 1; i++)
                            {
                                dx1 = 10; dy1 = d; dx2 = 90; dy2 = dy1;
                                e.Graphics.DrawLine(blackPen, dx1, dy1, dx2, dy2);
                                d = d + a;
                            }
                            d = d - a;
                            // 枠線描画：５本の縦線、ロット件数に応じた長さ、その１
                            dx1 = 10; dy1 = c; dx2 = 10; dy2 = d;
                            e.Graphics.DrawLine(blackPen, dx1, dy1, dx2, dy2);
                            // 枠線描画：５本の縦線、ロット件数に応じた長さ、その２
                            dx1 = 36; dy1 = c; dx2 = 36; dy2 = d;
                            e.Graphics.DrawLine(blackPen, dx1, dy1, dx2, dy2);
                            // 枠線描画：５本の縦線、ロット件数に応じた長さ、その３
                            dx1 = 50; dy1 = c; dx2 = 50; dy2 = d;
                            e.Graphics.DrawLine(blackPen, dx1, dy1, dx2, dy2);
                            // 枠線描画：５本の縦線、ロット件数に応じた長さ、その４
                            dx1 = 76; dy1 = c; dx2 = 76; dy2 = d;
                            e.Graphics.DrawLine(blackPen, dx1, dy1, dx2, dy2);
                            // 枠線描画：５本の縦線、ロット件数に応じた長さ、その５
                            dx1 = 90; dy1 = c; dx2 = 90; dy2 = d;
                            e.Graphics.DrawLine(blackPen, dx1, dy1, dx2, dy2);
                        }

                        try
                        {
                            // サトープリンタドライバにフォントを使用（事前にサトープリンタドライバにフォントの追加作業が必要）
                            int intX = 0;    // Ｘ座標
                            int intY = 0;    // Ｙ座標
                            int intFont = 0; // フォントサイズ
                            IntPtr hdc = e.Graphics.GetHdc(); // デバイスコンテキストを識別するハンドルを取得

                            y = 3;
                            // 見出文字：パックＩＤ
                            string idTypeString = (ltype2 == "Pack") ? "Pack ID:" : (ltype2 == "Carton") ? "Carton ID:" : (ltype2 == "Pallet") ? "Pallet ID:" : "ERROR!";
                            intX = 10; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, idTypeString, idTypeString.Length);
                            // 本文変数：パックＩＤ
                            intX = 35; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, trayPackCartonId, trayPackCartonId.Length);
                            // 本文バー：パックＩＤ
                            intX = 75; intY = y; intFont = 40;
                            SetFont(hdc, "DATAMATRIX", intFont);
                            PrintTextOut(hdc, intX, intY, trayPackCartonId, trayPackCartonId.Length);
                            // 見出文字：ＶＥＮＤＯＲ
                            y = y + a;
                            intX = 10; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, "Vendor:", "Vendor:".Length);
                            // 本文変数：ＶＥＮＤＯＲ
                            intX = 35; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, vendor, vendor.Length);
                            // 見出文字：産地
                            y = y + a;
                            intX = 10; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, "Origin:", "Origin:".Length);
                            // 本文変数：産地
                            intX = 35; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, "CN", "CN".Length);
                            // 見出文字：梱包日
                            y = y + a;
                            intX = 10; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, "Pack Date:", "Pack Date:".Length);
                            // 本文変数：梱包日
                            intX = 35; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, packdate, packdate.Length);
                            // 見出文字：一般Ｐ／Ｎ
                            y = y + a;
                            intX = 10; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, "P/N:", "P/N:".Length);
                            // 本文変数：一般Ｐ／Ｎ（ＦＯＸＣＯＮ ＯＲ ＰＥＧＡＴＲＯＮ で場合分け）
                            intX = 35; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, pn, pn.Length);
                            // 見出文字：ＭＯＤＥＬ
                            y = y + a;
                            intX = 10; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, "Model:", "Model:".Length);
                            // 本文変数：ＭＯＤＥＬ
                            intX = 35; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, model, model.Length);

                            // パレットの場合は、ロットごとの数量明細は不要のため、合計数量のみ表示する
                            if (ltype2 == "Pallet")
                            {
                                // 見出文字：ＱＵＡＮＴＩＴＹ（ＴＯＴＡＬ）
                                y = y + a;
                                intX = 10; intY = y; intFont = 40;
                                SetFont(hdc, "XM", intFont);
                                PrintTextOut(hdc, intX, intY, "Quantity:", "Quantity:".Length);
                                // 本文変数：ＱＵＡＮＴＩＴＹ（ＴＯＴＡＬ）
                                intX = 35; intY = y; intFont = 40;
                                SetFont(hdc, "XM", intFont);
                                PrintTextOut(hdc, intX, intY, qty, qty.Length);
                                return;
                            }

                            // 枠線内ヘッダー文字：ＤＡＴＥ ＣＯＤＥ
                            y = y + a + 1;
                            intX = 12; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, "Date CD", "Date CD".Length);
                            // 枠線内ヘッダー文字：ＱＴＹ
                            intX = 38; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, "Qty", "Qty".Length);
                            // 枠線内ヘッダー文字：ＤＡＴＥ ＣＯＤＥ
                            intX = 52; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, "Date CD", "Date CD".Length);
                            // 枠線内ヘッダー文字：ＱＴＹ
                            intX = 78; intY = y; intFont = 40;
                            SetFont(hdc, "XM", intFont);
                            PrintTextOut(hdc, intX, intY, "Qty", "Qty".Length);

                            // 枠線内データ（ＴＯＴＡＬ列があるため、－１）
                            for (int i = 0; i < dtLotSumary.Columns.Count - 1; i++)
                            {
                                datecode = dtLotSumary.Columns[i].ColumnName.ToString();
                                qty = dtLotSumary.Rows[0][i].ToString();
                                y = (i % 2 == 0)? y + a : y; 
                                int x1 = (i % 2 == 0)? 12 : 52;
                                int x2 = (i % 2 == 0)? 38 : 78;
                                // 本文変数：ＤＡＴＥ ＣＯＤＥ
                                intX = x1; intY = y; intFont = 40;
                                SetFont(hdc, "XM", intFont);
                                PrintTextOut(hdc, intX, intY, datecode, datecode.Length);
                                // 本文変数：ＱＴＹ
                                intX = x2; intY = y; intFont = 40;
                                SetFont(hdc, "XM", intFont);
                                PrintTextOut(hdc, intX, intY, qty, qty.Length);
                            }

                            // 後処理
                            DeleteObject(mFont);          // グラフィックオブジェクトを削除し、システムリソースの解放
                            SelectObject(hdc, mFontOld);  // デバイスコンテキストにオブジェクトの選択
                            e.Graphics.ReleaseHdc(hdc);   // デバイスコンテキストハンドルを解放
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show("Print failed." + System.Environment.NewLine + ex.Message, 
                                "Print Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        finally { blackPen.Dispose(); }
            */
        }

        // ラベル発行処理（パック・カートン・パレット、ペガトロン）
        private void printPackCartonPalletPega(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            // ペガトロンは、基本的には、ロットが１種類のパック・カートンしか、受け入れない
            //if (dtLotSumary.Columns.Count >= 3)
            //{
            //    DialogResult result = MessageBox.Show("There is more than 2 date code." + Environment.NewLine +
            //        "Do you print?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
            //    if (result == DialogResult.No) return;
            //}

            Pen blackPen = new Pen(Color.Black, 1);    // Penオブジェクトを作成(幅1の黒色
            e.Graphics.PageUnit = GraphicsUnit.Millimeter;  // Graphicsの設定をします

            try
            {
                // サトープリンタドライバにフォントを使用（事前にサトープリンタドライバにフォントの追加作業が必要）
                int intX = 0;    // Ｘ座標
                int intY = 0;    // Ｙ座標
                int intFont = 0; // フォントサイズ
                IntPtr hdc = e.Graphics.GetHdc(); // デバイスコンテキストを識別するハンドルを取得
                a = 6; // 上下文字始点Ｙ座標間の距離（ａ＝８、文字高さ＝５の場合は、文字間の幅＝３）
                b = 6; // 上部文字始点・下部バーコード始点のＹ座標間距離（ａ＝６、文字高さ＝５の場合は、文字とバーコードの幅＝１）

                // 見出文字：ＰＥＧＡＴＲＯＮ
                intX = 10; intY = 3; intFont = 55;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, "PEGATRON", "PEGATRON".Length);
                // 見出文字：ＶＥＮＤＯＲ
                y = 12;
                intX = 10; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, "Vendor:", "Vendor:".Length);
                // 本文変数：ＶＥＮＤＯＲd
                intX = 40; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, vendor, vendor.Length);
                // 本文バー：Ｐ／Ｎ
                y = y + a;
                intX = 40; intY = y; intFont = 40;
                SetFont(hdc, "CODE128", intFont);
                PrintTextOut(hdc, intX, intY, pn, pn.Length);
                // 見出文字：Ｐ／Ｎ
                y = y + a;
                intX = 10; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, "P/N:", "P/N:".Length);
                // 本文変数：ＰＥＧＡ Ｐ／Ｎ
                intX = 40; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, pn, pn.Length);
                // 見出文字： ＡＰＮ
                y = y + a;
                intX = 10; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, "APN:", "APN:".Length);
                // 本文変数：ＡＰＮ
                intX = 40; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, apn, apn.Length);
                // 本文文字：リビジョン
                intX = 70; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, "Rev:", "Rev:".Length);
                // 本文変数：リビジョン
                intX = 80; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, rev, rev.Length);
                // 見出文字：コンフィグ
                y = y + a;
                intX = 40; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, "Config:", "Config:".Length);
                // 本文変数：コンフィグ
                intX = 60; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, config, config.Length);
                // 見出文字：ディスクリプション
                y = y + a;
                intX = 10; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, "DESC:", "DESC:".Length);
                // 本文変数：ディスクリプション
                intX = 40; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, desc, desc.Length);
                // 本文バー：ＤＡＴＥ ＣＯＤＥ
                y = y + b;
                // 2016.07.28 PEC FUJIKI (実行場所の移動)
                datecode = dtLotSumary.Rows.Count >= 0 ? dtLotSumary.Columns[0].ColumnName.ToString() : string.Empty;
                intX = 40; intY = y; intFont = 40;
                SetFont(hdc, "CODE128", intFont);
                PrintTextOut(hdc, intX, intY, datecode, datecode.Length);
                // 見出文字：ＤＡＴＥ ＣＯＤＥ
                y = y + a;
                intX = 10; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, "Date Code:", "Date Code:".Length);
                // 本文変数：ＤＡＴＥ ＣＯＤＥ
                // 2016.07.28 PEC FUJIKI (実行場所の移動)
                // datecode = dtLotSumary.Rows.Count >= 0 ? dtLotSumary.Columns[0].ColumnName.ToString() : string.Empty;
                intX = 40; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, datecode, datecode.Length);
                //本文バー：バッチＮＯ（ＲＥＶ ＮＯ）
                y = y + b;
                intX = 40; intY = y; intFont = 40;
                SetFont(hdc, "CODE128", intFont);
                PrintTextOut(hdc, intX, intY, rev, rev.Length);
                // 見出文字：バッチＮＯ（ＲＥＶ ＮＯ）
                y = y + a;
                intX = 10; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, "Batch(Rev):", "Batch(Rev):".Length);
                // 本文変数：バッチＮＯ（ＲＥＶ ＮＯ）
                intX = 40; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, rev, rev.Length);
                // 本文バー： ＱＴＹ
                y = y + b;
                intX = 40; intY = y; intFont = 40;
                SetFont(hdc, "CODE128", intFont);
                PrintTextOut(hdc, intX, intY, qty, qty.Length);
                //// 見出文字：ＱＴＹ
                y = y + a;
                intX = 10; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, "QTY:", "QTY:".Length);
                // 本文変数：ＱＴＹ
                qty = dtLotSumary.Rows.Count >= 0 ? dtLotSumary.Rows[0]["total"].ToString() : string.Empty;
                intX = 40; intY = y; intFont = 50;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, qty, qty.Length);
                // 本文変数：右下太文字
                intX = 65; intY = y; intFont = 40;
                SetFont(hdc, "XM", intFont);
                PrintTextOut(hdc, intX, intY, footnote, footnote.Length);

                // 後処理
                DeleteObject(mFont);          // グラフィックオブジェクトを削除し、システムリソースの解放
                SelectObject(hdc, mFontOld);  // デバイスコンテキストにオブジェクトの選択
                e.Graphics.ReleaseHdc(hdc);   // デバイスコンテキストハンドルを解放
            }
            catch { MessageBox.Show("Label printing failed.", "Print Result", MessageBoxButtons.OK); }
            finally { blackPen.Dispose(); }
        }

        // 論理フォント作成
        private void SetFont(System.IntPtr control, string fontFamily, int fontSize)
        {
            mFont = CreateFont(fontSize * -1, 0, 0, 0, 400, false, false, false, 1, 0, 0, 0, 0, fontFamily);
            mFontOld = SelectObject(control, mFont);
        }

        // 文字列描写
        private void PrintTextOut(System.IntPtr hdc, int intX, int intY, string strPrint, int intCount)
        {
            // SATO SR408(203dpi)より1dot辺りのMillimeterを指定
            double dbldotmm = 0.125;
            int intPixelX = Convert.ToInt16(intX / dbldotmm);
            int intPixelY = Convert.ToInt16(intY / dbldotmm);
            TextOut(hdc, intPixelX, intPixelY, strPrint, intCount);
        }

        // 印刷データを指定の位置に出力
        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        private extern static int TextOut(IntPtr hdc, int nXStart, int nYStart, string lpString, int cbString);

        // 論理フォントを作成
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private extern static System.IntPtr CreateFont(int nHeight, int nWidth, int nEscapement,
            int nOrientation, int fnWeight, bool fdwItalic, bool fdwUnderline, bool fdwStrikeOut, int fdwCharSet,
            int fdwOutputPrecision, int fdwClipPrecision, int fdwQuality, int fdwPitchAndFamily, string lpszFace);

        // デバイスコンテキストにオブジェクトを選択
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private extern static System.IntPtr SelectObject(System.IntPtr hObject, System.IntPtr hFont);

        // グラフィックオブジェクトを削除し、システムリソースの解放
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private extern static bool DeleteObject(System.IntPtr hObject);

        // 設定テキストファイルの読み込み
        private string readIni(string s, string k, string cfs)
        {
            StringBuilder retVal = new StringBuilder(255);
            string section = s;
            string key = k;
            string def = String.Empty;
            int size = 255;
            int strref = GetPrivateProfileString(section, key, def, retVal, size, cfs);
            return retVal.ToString();
        }
        // Windows API をインポート
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filepath);
    }
}
