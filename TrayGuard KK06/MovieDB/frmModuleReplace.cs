using System;
using System.Data;
using System.Windows.Forms;
using System.Security.Permissions;
using System.Drawing;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace TrayGuard
{
    public partial class frmModuleReplace : Form
    {
        //�e�t�H�[��frmTray�փC�x���g������A���i�f���Q�[�g�j
        public delegate void RefreshEventHandler(object sender, EventArgs e);
        public event RefreshEventHandler RefreshEvent;

        // �v�����g�p�e�L�X�g�t�@�C���̕ۑ��p�t�H���_���A��{�ݒ�t�@�C���Őݒ肷��
        string appconfig = System.Environment.CurrentDirectory + "\\info.ini";
        string productconfig = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\tray_guard_desktop.ini";
        string outPath = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\NTRS Log\";

        //���̑��񃍁[�J���ϐ�
        DataTable dtModule;
        string trayId;
        bool formReturnMode;
        bool sound;
        bool testerNg;
        string totalSwitch;
        string OK2ShipCheckSwitch;
        string forcedNGSwitch;

        // ���i�V���A���\���v�f�`�F�b�N�p�ϐ�
        string plant;
        string year;
        string week;
        string day;
        string line;
        string eeee;
        string revision;
        string mass;
        string flexure;
        string cover_base;
        string dframe;
        string fpc;
        string shift;

        // �R���X�g���N�^
        public frmModuleReplace()
        {
            InitializeComponent();

            // ���i�V���A���\���v�f�́A�ϐ��ւ̊i�[
            plant = readIni("MODULE NUMBERING CHECK", "PLANT", productconfig);
            year = readIni("MODULE NUMBERING CHECK", "YEAR", productconfig);
            week = readIni("MODULE NUMBERING CHECK", "WEEK", productconfig);
            day = readIni("MODULE NUMBERING CHECK", "DAY", productconfig);
            line = readIni("MODULE NUMBERING CHECK", "LINE", productconfig);
            eeee = readIni("MODULE NUMBERING CHECK", "EEEE", productconfig);
            revision = readIni("MODULE NUMBERING CHECK", "REVISION", productconfig);
            mass = readIni("MODULE NUMBERING CHECK", "MASS", productconfig);
            flexure = readIni("MODULE NUMBERING CHECK", "FLEXURE", productconfig);
            cover_base = readIni("MODULE NUMBERING CHECK", "COVER/BASE", productconfig);
            dframe = readIni("MODULE NUMBERING CHECK", "D-FRAME", productconfig);
            fpc = readIni("MODULE NUMBERING CHECK", "FPC", productconfig);
            shift = readIni("MODULE NUMBERING CHECK", "SHIFT", productconfig);
        }

        // ���[�h���̏���
        private void Form4_Load(object sender, EventArgs e)
        {
            // ���i�E�f�[�^�A�}�b�`���O�@�\�p�ݒ���e�̎擾
            forcedNGSwitch = readIni("MODULE-DATA MATCHING", "FORCED NG SWITCH", appconfig);
            OK2ShipCheckSwitch = readIni("MODULE-DATA MATCHING", "OK2SHIP CHECK SWITCH", appconfig);
            totalSwitch = (OK2ShipCheckSwitch == "OFF" && forcedNGSwitch == "OFF") ? "OFF" : "ON";

            // ���O�p�t�H���_�̍쐬
            if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath);

            //�t�H�[���̏ꏊ���w��
            this.Left = 450;
            this.Top = 100;
            dtModule = new DataTable();
            defineDatatable(ref dtModule);
            updateDataGridViews(dtModule, ref dgvModule);

            // ���݂̈ꎞ�o�^������ϐ��֕ێ�����
            int okCount = getOkCount(dtModule);
            txtRow.Text = okCount.ToString();
        }

        // �T�u�v���V�[�W���F �e�X�^�[�p�r�d�k�d�b�s�P�[�X��̍쐬
        private string makeSqlCaseClause(string criteria)
        {
            string sql = "case ";
            foreach (string c in criteria.Split(','))
            { sql += "when c.process_cd like " + VBS.Left(c, c.Length - 1) + "%' then " + c + " "; };
            sql += "else c.process_cd end as tester_id ";
            System.Diagnostics.Debug.Print(sql);
            return sql;
        }

        // �T�u�v���V�[�W���F �e�X�^�[�p�v�g�d�q�d��̍쐬
        private string makeSqlWhereClause(string criteria)
        {
            string sql = "where ";
            foreach (string c in criteria.Split(','))
            { sql += "c.process_cd like " + VBS.Left(c, c.Length - 1) + "%' or "; };
            sql = VBS.Left(sql, sql.Length - 3);
            System.Diagnostics.Debug.Print(sql);
            return sql;
        }

        // �T�u�v���V�[�W��: �c�s�̒�`
        private void defineDatatable(ref DataTable dt)
        {
            dt.Columns.Add("module_id", typeof(string));
            dt.Columns.Add("lot", typeof(string));
            dt.Columns.Add("bin", typeof(string));
            dt.Columns.Add("tester_id", typeof(string));
            dt.Columns.Add("test_result", typeof(string));
            dt.Columns.Add("test_date", typeof(DateTime));
            dt.Columns.Add("r_mode", typeof(string));
        }

        // �T�u�v���V�[�W���F�e�t�H�[���ŌĂяo���A�e�t�H�[���̏����A�e�L�X�g�{�b�N�X�֊i�[���Ĉ����p��
        public void updateControls(string tray, string serno, int row, bool mode, string binselect)
        {
            trayId = tray;
            txtBefore.Text = serno;
            txtRow.Text = row.ToString();
            formReturnMode = mode;
            txtBinSelect.Text = binselect;
        }

        // �T�u�v���V�[�W���F�f�[�^�O���b�g�r���[�̍X�V
        private void updateDataGridViews(DataTable dt, ref DataGridView dgv)
        {
            // �f�[�^�O���b�g�r���[�ւc�s�`�`�s�`�a�k�d���i�[
            dgv.DataSource = dt;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            // �e�X�g���ʂ��e�`�h�k�܂��̓��R�[�h�Ȃ��̃V���A�����}�[�L���O����
            if (dt.Rows.Count <= 0) return;
 
            if (dgv["test_result", 0].Value.ToString() == "FAIL" || dgv["test_result", 0].Value.ToString() == "NG" || dgv["test_result", 0].Value.ToString() == string.Empty)
            {
                dgv["tester_id", 0].Style.BackColor = Color.Red;
                dgv["test_result", 0].Style.BackColor = Color.Red;
                dgv["test_date", 0].Style.BackColor = Color.Red;
                soundAlarm();
                testerNg = true;
            }
            else
            {
                dgv["tester_id", 0].Style.BackColor = Color.FromKnownColor(KnownColor.Window);
                dgv["test_result", 0].Style.BackColor = Color.FromKnownColor(KnownColor.Window);
                dgv["test_date", 0].Style.BackColor = Color.FromKnownColor(KnownColor.Window);
                testerNg = false;
            }
        }
        // �T�u�v���V�[�W���F�V���A���ԍ��d���Ȃ��̂o�`�r�r�����擾����
        private int getOkCount(DataTable dt)
        {
            DataTable distinct = dt.DefaultView.ToTable(true, new string[] { "module_id", "test_result" });
            DataRow[] dr;
            if (totalSwitch == "ON") dr = distinct.Select("test_result in ('OK','n/a')"); //distinct.Select(); �e�`�h�k�������A�����i�K�̐ݒ�
            else dr = distinct.Select();  // �}�b�`���O�X�C�b�`�I�t�̏ꍇ�́A�e�X�g���ʂɊ֌W�Ȃ��s�����J�E���g����

            return dr.Length;
        }

        // �e�X�g���ʂ��i�[����N���X
        public class TestResult
        {
            public string tester_id { get; set; }
            public string test_result { get; set; }
            public string test_date { get; set; }
        }
        // �e�X�g���ʂ̃v���Z�X�R�[�h�݂̂��i�[����N���X
        public class ProcessList
        {
            public string tester_id { get; set; }
        }

        // �ύX�ヂ�W���[�����X�L�������ꂽ�Ƃ��̏���
        private void txtAfter_KeyDown(object sender, KeyEventArgs e)
        {
            // �G���^�[�L�[�̏ꍇ�A�e�L�X�g�{�b�N�X�̌������P�V���܂��͂Q�S���̏ꍇ�̂݁A�������s��
            if (e.KeyCode != Keys.Enter) return;
            if (txtAfter.Text.Length != 17 && txtAfter.Text.Length != 24) return;

            // �f�[�^�e�[�u���̃N���A
            dtModule.Rows.Clear();

            // �a�`�r�d�V���A������g�n�n�o�V���A�����擾���i�X�e�b�v�P�j�A�����̃V���A���ɊY������A�e�X�g���ʁE�v���Z�X���E�e�X�g�������擾����i�X�e�b�v�Q�A�X�e�b�v�R�j
            TfSQL tf = new TfSQL();
            DataTable dt = new DataTable();
            string log = string.Empty;
            string module = txtAfter.Text;
            string mdlShort = VBS.Left(module, 17);
            string mdlNtrs = string.Empty;
            string mdlOK2ShipResult = string.Empty;
            // 2016.08.18 FUJIKI FORCED NG CHECK ��ǉ�
            string mdlForcedNGResult = string.Empty;
            // 2016.08.18 FUJIKI FORCED NG CHECK ��ǉ�
            string scanTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            string displayAll = string.Empty;   // ���O�p
            string textResult = "PASS";
            string mdlSerialBin = string.Empty;
            string textSelectBin = string.Empty;

            // 2016.08.18 FUJIKI FORCED NG CHECK ��ǉ�
            if (forcedNGSwitch == "ON" && OK2ShipCheckSwitch == "ON")
            {
                string sql0 = "select serial from forceddata where serial = '" + mdlShort + "' and result = 1";
                mdlForcedNGResult = tf.sqlExecuteScalarStringOK2Ship(sql0);

                if (!string.IsNullOrEmpty(mdlForcedNGResult))
                {
                    textResult = "NG";
                    displayAll = "NG SERIAL";
                }
                else
                {
                    // Short Factory Serial No �̌���
                    string sql1 = "select received_result from data where serial = '" + mdlShort + "' and received_result = '0'";
                    mdlOK2ShipResult = tf.sqlExecuteScalarStringOK2Ship(sql1);

                    // �r����������
                    if (string.IsNullOrEmpty(mdlOK2ShipResult))
                    {
                        mdlOK2ShipResult = "NG";
                        textResult = "NG";
                    }
                    else
                    {
                        mdlOK2ShipResult = "OK";
                    }
                    displayAll = displayAll + "OK2:" + mdlOK2ShipResult;
                }
            }
            else
            {
                mdlOK2ShipResult = string.Empty;
                textResult = "n/a";
                displayAll = "test function off";
            }

            // ��������̃e�[�u���Ƀ��R�[�h��ǉ�
            DataRow dr = dtModule.NewRow();
            dr["module_id"] = module;
            dr["lot"] = VBS.Left(module, 8);
            dr["bin"] = mdlSerialBin;
            dr["tester_id"] = displayAll;
            dr["test_result"] = textResult;
            dr["test_date"] = DateTime.ParseExact(scanTime, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture); ;
            dr["r_mode"] = formReturnMode ? "T" : "F";
            dtModule.Rows.Add(dr);

            // �A�v���P�[�V�����t�H���_�ɁA���t�ƃe�X�g���ʂ̃��O��t����
            log = Environment.NewLine + scanTime + "," + module + "," + displayAll + ":" + textResult;

            // �������t�̃t�@�C�������݂���ꍇ�͒ǋL���A���݂��Ȃ��ꍇ�̓t�@�C�����쐬�ǋL����iAppendAllText ������Ă����j
            try
            {
                string outFile = outPath + DateTime.Today.ToString("yyyyMMdd") + ".txt";
                System.IO.File.AppendAllText(outFile, log, System.Text.Encoding.GetEncoding("UTF-8"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // ���݂̈ꎞ�o�^������ϐ��֕ێ�����
            int okCount = getOkCount(dtModule);
            txtRow.Text = okCount.ToString();

            // �f�[�^�O���b�g�r���[�̍X�V
            updateDataGridViews(dtModule, ref dgvModule);

        }

        // �T�u�v���V�[�W���F�V���A���̍\���v�f�̃p�^�[�����K�����A���[�U�[�f�X�N�g�b�v�̐ݒ�t�@�C�����g�p���Ċm�F����
        private string matchSerialNumberingPattern(string serial)
        {
            string result = string.Empty;

            // �ݒ�t�@�C�� �k�h�m�d �� �w�w�w �̏ꍇ�́A���؂��Ȃ�
            // if (line == "XXX") return string.Empty;
            if      (!plant.Equals("XXX")      && VBS.Mid(serial,  1, 3) != plant)      return "Plant '"      + VBS.Mid(serial,  1, 3) + "'";
            else if (!year.Equals("XXX")       && VBS.Mid(serial,  4, 1) != year)       return "Year '"       + VBS.Mid(serial,  4, 1) + "'";
            else if (!week.Equals("XXX")       && VBS.Mid(serial,  5, 2) != week)       return "Week '"       + VBS.Mid(serial,  5, 2) + "'";
            else if (!day.Equals("XXX")        && VBS.Mid(serial,  7, 1) != day)        return "Day '"        + VBS.Mid(serial,  7, 1) + "'";
            else if (!line.Equals("XXX")       && VBS.Mid(serial,  8, 1) != line)       return "Line '"       + VBS.Mid(serial,  8, 1) + "'";
            else if (!eeee.Equals("XXX")       && VBS.Mid(serial, 12, 4) != eeee)       return "4E '"         + VBS.Mid(serial, 12, 4) + "'";
            else if (!revision.Equals("XXX")   && VBS.Mid(serial, 16, 1) != revision)   return "Revision '"   + VBS.Mid(serial, 16, 1) + "'";
            else if (!mass.Equals("XXX")       && VBS.Mid(serial, 19, 1) != mass)       return "Mass '"       + VBS.Mid(serial, 19, 1) + "'";
            else if (!flexure.Equals("XXX")    && VBS.Mid(serial, 20, 1) != flexure)    return "Flexure '"    + VBS.Mid(serial, 20, 1) + "'";
            else if (!cover_base.Equals("XXX") && VBS.Mid(serial, 21, 1) != cover_base) return "Cover/base '" + VBS.Mid(serial, 21, 1) + "'";
            else if (!dframe.Equals("XXX")     && VBS.Mid(serial, 22, 1) != dframe)     return "D-Frame '"    + VBS.Mid(serial, 22, 1) + "'";
            else if (!fpc.Equals("XXX")        && VBS.Mid(serial, 23, 1) != fpc)        return "FPC '"        + VBS.Mid(serial, 23, 1) + "'";
            else if (!shift.Equals("XXX")      && VBS.Mid(serial, 24, 1) != shift)      return "Shift '"      + VBS.Mid(serial, 24, 1) + "'";
            else return string.Empty;
        }

        // �o�^�ς݂̃V���A������т��̕t�я����A�t�o�c�`�s�d���Œu��������
        private void btnReplace_Click(object sender, EventArgs e)
        {
            if (dtModule.Rows.Count <= 0) return; //if (testerNg || dtModule.Rows.Count <= 0) 
            if (!txtRow.Text.Equals("1"))
            {
                MessageBox.Show("Please check module-id status.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }

            string mdlBefore = txtBefore.Text;
            string mdlAfter = dtModule.Rows[0]["module_id"].ToString();

            // �d���m�F�������s��
            TfSQL tf = new TfSQL();
            string dbDuplicate = tf.sqlModuleDuplicateCheck(dtModule);
            if (dbDuplicate != string.Empty)
            {
                for (int i = 0; i < dgvModule.Rows.Count; ++i)
                {
                    if (dgvModule["module_id", i].Value.ToString() == dbDuplicate)
                        dgvModule["module_id", i].Style.BackColor = Color.Red;
                }
                // soundAlarm();
                btnReplace.Enabled = false;
                return;
            }

            // �X�V����
            string sql = "update t_module set " +
                "module_id ='" + (string)dtModule.Rows[0]["module_id"].ToString() + "', " +
                "lot ='" + dtModule.Rows[0]["lot"].ToString() + "', " +
                "tester_id ='" + dtModule.Rows[0]["tester_id"].ToString() + "', " +
                "test_result ='" + dtModule.Rows[0]["test_result"].ToString() + "', " +
                "test_date ='" + dtModule.Rows[0]["test_date"].ToString() + "', " +
                "r_mode ='" + dtModule.Rows[0]["r_mode"].ToString() + "' " +
                "where module_id = '" + mdlBefore + "' and tray_id ='" + trayId + "'";
            System.Diagnostics.Debug.Print(sql);
            bool res = tf.sqlExecuteNonQuery(sql, false);

            if (res)
            {
                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
                btnReplace.Enabled = false;
                txtAfter.Enabled = false;
                txtRow.Text = string.Empty;
                this.Focus();
                MessageBox.Show("The replacement was successful.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
            }
        }

        // �ݒ�e�L�X�g�t�@�C���̓ǂݍ���
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
        // Windows API ���C���|�[�g
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filepath);

        //����{�^����V���[�g�J�b�g�ł̏I���������Ȃ�
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x112;
            const long SC_CLOSE = 0xF060L;
            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt64() & 0xFFF0L) == SC_CLOSE) { return; }
            base.WndProc(ref m);
        }

        //MP3�t�@�C���i����͌x�����j���Đ�����
        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        private static extern int mciSendString(String command,
           StringBuilder buffer, int bufferSize, IntPtr hwndCallback);

        private string aliasName = "MediaFile";

        private void soundAlarm()
        {
            string currentDir = System.Environment.CurrentDirectory;
            string fileName = currentDir + @"\warning.mp3";
            string cmd;

            if (sound)
            {
                cmd = "stop " + aliasName;
                mciSendString(cmd, null, 0, IntPtr.Zero);
                cmd = "close " + aliasName;
                mciSendString(cmd, null, 0, IntPtr.Zero);
                sound = false;
            }

            cmd = "open \"" + fileName + "\" type mpegvideo alias " + aliasName;
            if (mciSendString(cmd, null, 0, IntPtr.Zero) != 0) return;
            cmd = "play " + aliasName;
            mciSendString(cmd, null, 0, IntPtr.Zero);
            sound = true;
        }

        // ���̃t�H�[���Ƃ̐���������邽�߁A�L�����Z���{�^����݂���
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}