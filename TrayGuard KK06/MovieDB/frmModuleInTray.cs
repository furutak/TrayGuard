using System;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace TrayGuard
{
    public partial class frmModuleInTray : Form
    {
        //�e�t�H�[��frmTray�փC�x���g������A���i�f���Q�[�g�j
        public delegate void RefreshEventHandler(object sender, EventArgs e);
        public event RefreshEventHandler RefreshEvent;

        // �v�����g�p�e�L�X�g�t�@�C���̕ۑ��p�t�H���_���A��{�ݒ�t�@�C���Őݒ肷��
        string appconfig = System.Environment.CurrentDirectory + @"\info.ini";
        string productconfig = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\tray_guard_desktop.ini";
        string outPath = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\NTRS Log\";

        //���̑��A�񃍁[�J���ϐ�
        DataTable dtModule;
        DataTable dtLot;
        DataTable dtModuleBeforeRefill;
        int countBeforeRefill;
        bool formAddMode;
        bool formRefillMode;
        bool formReturnMode;
        bool formPartialCancelMode;
        bool formReprintMode;
        bool trayIdCanceled;
        bool trayIdPacked;
        string totalSwitch;
        string userRole;
        int okCount;
        int capacity = 24;
        string maxLot;
        DateTime registerDate;
        bool sound;
        int position;
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
        public frmModuleInTray()
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
        private void frmModule_Load(object sender, EventArgs e)
        {
            this.Text = this.Text + " " + Assembly.GetExecutingAssembly().GetName().Version;
            forcedNGSwitch = readIni("MODULE-DATA MATCHING", "FORCED NG SWITCH", appconfig);
            OK2ShipCheckSwitch = readIni("MODULE-DATA MATCHING", "OK2SHIP CHECK SWITCH", appconfig);
            totalSwitch = (OK2ShipCheckSwitch == "OFF" && forcedNGSwitch == "OFF") ? "OFF" : "ON";

            // ���O�p�t�H���_�̍쐬
            if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath); 

            // ���t�H�[���̕\���ꏊ���w��
            this.Left = 250;
            this.Top = 20;
            if (position == 2) { this.Left = 400; this.Top = 30; }
            else if (position == 3) { this.Left = 450; this.Top = 40; }
            else if (position == 4) { this.Left = 500; this.Top = 50; }

            // �e�폈���p�̃e�[�u���𐶐����A�f�[�^��ǂݍ���
            dtModule = new DataTable();
            defineModuleTable(ref dtModule);
            if (!formAddMode) readModuleInfo(ref dtModule);

            // �O���b�g�r���[�̍X�V
            updateDataGridViews(dtModule, ref dgvModule);

            // �ǉ����[�h�A�{�����[�h�̐؂�ւ�
            changeMode();

            // �o�b�`�R���{�{�b�N�X�̐ݒ�
            setShiftComboBox();
        }

        // �T�u�v���V�[�W���F �e�X�^�[�p�r�d�k�d�b�s�P�[�X��̍쐬
        private string makeSqlCaseClause(string criteria)
        {
            string sql = " case ";
            foreach (string c in criteria.Split(','))
            { sql += "when c.process_cd like " + VBS.Left(c, c.Length - 1) + "%' then " + c + " ";  };
            sql += "else c.process_cd end as tester_id ";
            System.Diagnostics.Debug.Print(sql);
            return sql;
        }

        // �T�u�v���V�[�W���F �e�X�^�[�p�v�g�d�q�d��̍쐬
        private string makeSqlWhereClause(string criteria)
        {
            string sql = " where ";
            foreach (string c in criteria.Split(','))
            { sql += "c.process_cd like " + VBS.Left(c, c.Length - 1) + "%' or "; };
            sql = VBS.Left(sql, sql.Length - 3);
            System.Diagnostics.Debug.Print(sql);
            return sql;
        }

        // �T�u�v���V�[�W���F �o�b�`�R���{�{�b�N�X�̐ݒ�
        private void setShiftComboBox()
        {
            string sql = "select content from t_criteria where criteria = 'BIN_SHIFT' order by content";
            TfSQL tf = new TfSQL();
            tf.getComboBoxData(sql, ref cmbBinShift);
        }

        // �T�u�v���V�[�W���F �ǉ����[�h�A�{�����[�h�̐؂�ւ�
        private void changeMode()
        {
            // �ǉ����[�h�̏ꍇ�i�[�U���[�h�̏ꍇ�́A�c�d�k�d�s�d�r�d�k�d�b�s�h�n�m�͖����j
            if (formAddMode)
            {
                cmbBinShift.Enabled = formRefillMode ? false : true; 
                txtModuleId.Enabled = true;

                btnRegisterTray.Enabled = false;
                btnCancelTray.Enabled = false;
                btnReplaceModule.Enabled = false;
                btnReprintLabel.Enabled = false;
                btnRefillTray.Enabled = false;
                btnClose.Enabled = true;
                btnDeleteSelection.Enabled = formRefillMode ? false : true;
                //btnChangeCapacity.Enabled = (userRole == "super" && txtLoginDept.Text == "PC") ? true : false;
                btnChangeCapacity.Enabled = (userRole == "super") ? true : false;
            }
            // �{�����[�h�̏ꍇ
            else
            {
                cmbBinShift.Enabled = false;
                txtModuleId.Enabled = false;

                btnRegisterTray.Enabled = false;
                btnCancelTray.Enabled = false;
                btnReplaceModule.Enabled = false;
                // 2016.08.10 FUJII  �ăv�����g���[�h��ǉ��ifrmTray�ɂāA���W���[���h�c���L�[�Ƃ��āA�g���[�h�c����肵���ꍇ�̂݁A�Ĉ���\�j
                btnReprintLabel.Enabled = trayIdCanceled ? false : ((formReprintMode && userRole == "super") ? true : false); 

                btnRefillTray.Enabled = (trayIdCanceled || trayIdPacked || txtLoginDept.Text == "MFG") ? false : true; 
                btnClose.Enabled = true;
                btnDeleteSelection.Enabled = false;
                //btnChangeCapacity.Enabled = (userRole == "super" && txtLoginDept.Text == "PC") ? true : false;
                btnChangeCapacity.Enabled = (userRole == "super") ? true : false;
                if (userRole == "super")
                {
                    btnCancelTray.Enabled = (trayIdCanceled || trayIdPacked) ? false : true;
                    btnReplaceModule.Enabled = (trayIdCanceled || trayIdPacked) ? false : true; 
                }
            }

            // �ꕔ�L�����Z�����[�h�̏ꍇ�ADELETE SELECTION �ȊO�͖���
            if (formPartialCancelMode)
            {
                cmbBinShift.Enabled = false;
                txtModuleId.Enabled = false;

                btnRegisterTray.Enabled = false;
                btnCancelTray.Enabled = false;
                btnReplaceModule.Enabled = false;
                btnReprintLabel.Enabled = false;
                btnRefillTray.Enabled = false;
                btnClose.Enabled = true;
                btnDeleteSelection.Enabled = true;
                btnChangeCapacity.Enabled = false;
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

        // �T�u�v���V�[�W���F�e�t�H�[���ŌĂяo���A�e�t�H�[���̏����A�e�L�X�g�{�b�N�X�֊i�[���Ĉ����p��
        public void updateControls(string tid, DateTime tdate, string uid, string uname, string udept, string urole, string shift, 
            bool addMode, bool returnMode, bool canceled, bool packed, int pos, bool partialCancelMode, bool reprintMode)
        {
            txtTrayId.Text = tid;
            dtpRegisterDate.Value = tdate;
            txtLoginName.Text = uname;
            txtLoginDept.Text = udept;
            userRole = urole;
            if (!addMode) cmbBinShift.Text = shift;
            formAddMode = addMode;
            formReturnMode = returnMode;
            trayIdCanceled = canceled;
            trayIdPacked = packed;
            position = pos;
            formPartialCancelMode = partialCancelMode;
            formReprintMode = reprintMode;
        }

        // �T�u�v���V�[�W���F�f�[�^�e�[�u���̒�`
        private void defineModuleTable(ref DataTable dt)
        {
            dt.Columns.Add("module_id", typeof(string));
            dt.Columns.Add("lot", typeof(string));
            dt.Columns.Add("bin", typeof(string));
            dt.Columns.Add("tester_id", typeof(string));
            dt.Columns.Add("test_result", typeof(string));
            dt.Columns.Add("test_date", typeof(DateTime));
            dt.Columns.Add("r_mode", typeof(string));
        }

        // �T�u�v���V�[�W���F�c�a����f�[�^�e�[�u���ւ̓ǂݍ���
        private void readModuleInfo(ref DataTable dt)
        {
            dt.Rows.Clear();
            string sql = "select module_id, lot, bin, tester_id, test_result, test_date, r_mode " +
                "from t_module where tray_id='" + txtTrayId.Text + "'";
            TfSQL tf = new TfSQL();
            System.Diagnostics.Debug.Print(sql);
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql, ref dt);
        }

        // �T�u�v���V�[�W���F�f�[�^�O���b�g�r���[�̍X�V
        private void updateDataGridViews(DataTable dt1, ref DataGridView dgv1)
        {
            // �f�[�^�O���b�g�r���[�ւc�s�`�`�s�`�a�k�d���i�[
            dgv1.DataSource = dt1;
            dgv1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            // �e�X�g���ʂ��e�`�h�k�܂��̓��R�[�h�Ȃ��̃V���A�����}�[�L���O����i�}�b�`���O�X�C�b�`�A�I���̏ꍇ�j
            if (totalSwitch == "ON") colorViewForFailAndBlank(ref dgv1);

            // �d�����R�[�h�A����тP�Z���Q�d���͂��}�[�L���O����
            colorViewForDuplicateSerial(ref dgv1);

            //�s�w�b�_�[�ɍs�ԍ���\������
            for (int i = 0; i < dgv1.Rows.Count; i++) dgv1.Rows[i].HeaderCell.Value = (i + 1).ToString();

            //�s�w�b�_�[�̕����������߂���
            dgv1.AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders);

            // ��ԉ��̍s��\������i�C�����C���j
            if (dgv1.Rows.Count >= 1) dgv1.FirstDisplayedScrollingRowIndex = dgv1.Rows.Count - 1;

            // ���݂̈ꎞ�o�^������ϐ��֕ێ�����
            okCount = getOkCount(dt1);
            txtOkCount.Text = okCount.ToString() + "/" + capacity.ToString();

            // ���b�g�W�v�O���b�h�r���[���X�V���A���ʂ̍ł��������b�g��ێ�����
            maxLot = updateLotSummary(dt1);

            // ���R�[�h���ƃg���C�e�ʂɂ��A�{�^���ƃR���g���[���̐���A�i�ǉ����[�h�̏ꍇ�j
            if (formAddMode)
            {
                if (okCount == capacity)
                {
                    cmbBinShift.Enabled = formRefillMode ? false : true;
                    txtModuleId.Enabled = false;
                    btnRegisterTray.Enabled = true;
                    btnDeleteSelection.Enabled = formRefillMode ? false : true;
                    //btnChangeCapacity.Enabled = (userRole == "super" && txtLoginDept.Text == "PC") ? true : false;
                    btnChangeCapacity.Enabled = (userRole == "super") ? true : false;
                }
                else
                {
                    cmbBinShift.Enabled = formRefillMode ? false : true;
                    txtModuleId.Enabled = true;
                    btnRegisterTray.Enabled = false;
                    btnDeleteSelection.Enabled = formRefillMode ? false : true;
                    //btnChangeCapacity.Enabled = (userRole == "super" && txtLoginDept.Text == "PC") ? true : false;
                    btnChangeCapacity.Enabled = (userRole == "super") ? true : false;
                    txtModuleId.SelectAll(); // �A���X�L�����p
                }
            }
        }

        // �T�u�v���V�[�W���F�V���A���ԍ��d���Ȃ��̂o�`�r�r�����擾����
        private int getOkCount(DataTable dt)
        {
            DataTable distinct = dt.DefaultView.ToTable(true, new string[] { "module_id", "test_result" });
            DataRow[] dr;
            if (totalSwitch == "ON") dr = distinct.Select("test_result in ('PASS','n/a')"); //distinct.Select(); �e�`�h�k�������A�����i�K�̐ݒ�
            else dr = distinct.Select();  // �}�b�`���O�X�C�b�`�I�t�̏ꍇ�́A�e�X�g���ʂɊ֌W�Ȃ��s�����J�E���g����

            return dr.Length;
        }

        // �T�u�v���V�[�W���F���b�g�W�v�O���b�h�r���[���X�V���A���ʂ̍ł��������b�g�ԍ���Ԃ�
        public string updateLotSummary(DataTable dt)
        {
            if (dt.Rows.Count <= 0)
            {
                dtLot = new DataTable();
                dgvLotSummary.DataSource = dtLot;
                return string.Empty;
            }
                
            var query = from e in dt.AsEnumerable()
                        group e by e.Field<string>("lot") into Summary
                        orderby Summary.Count() descending
                        select new {lot = Summary.Key, qty = Summary.Count()};

            // �O���b�g�r���[�f�[�^�\�[�X�̍폜�ƁA�V�f�[�^�e�[�u���̐���
            dgvLotSummary.DataSource = null;
            dgvLotSummary.Refresh();
            dtLot = new DataTable();
            // ��̒ǉ�
            foreach (var q in query) dtLot.Columns.Add(q.lot, typeof(int));
            dtLot.Columns.Add("total", typeof(int));
            // �s�̒ǉ�
            dtLot.Rows.Add(); 
            foreach (var q in query) dtLot.Rows[0][q.lot] = q.qty;
            dtLot.Rows[0]["total"] = query.Sum(a => a.qty);
 
            dgvLotSummary.DataSource = dtLot;
            return query.First().lot;
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

        // �V���A�����X�L�������ꂽ���̏���            
        private void txtModuleId_KeyDown(object sender, KeyEventArgs e)
        {
            // �G���^�[�L�[�̏ꍇ�A�e�L�X�g�{�b�N�X�̌������P�V���܂��͂Q�S���̏ꍇ�̂݁A�������s��
            if (e.KeyCode != Keys.Enter) return;
            if (txtModuleId.Text.Length != 17 && txtModuleId.Text.Length != 24) return;

            // �a�`�r�d�V���A������g�n�n�o�V���A�����擾���i�X�e�b�v�P�j�A�����̃V���A���ɊY������A�e�X�g���ʁE�v���Z�X���E�e�X�g�������擾����i�X�e�b�v�Q�A�X�e�b�v�R�j
            TfSQL tf = new TfSQL();
            DataTable dt = new DataTable();
            string log = string.Empty;
            string module = txtModuleId.Text;
            string mdlShort = VBS.Left(module, 17);
            string mdlNtrs = string.Empty;
            string mdlOK2ShipResult = string.Empty;
            // 2016.08.18 FUJIKI FORCED NG CHECK ��ǉ�
            string mdlForcedNGResult = string.Empty;
            // 2016.08.18 FUJIKI FORCED NG CHECK ��ǉ�
            string scanTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            string displayAll = string.Empty;   // ���O�p
            DataRow dr = dtModule.NewRow();
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

            // 2017.03.08 FUJII  �V���A���\���v�f�̃`�F�b�N���I�t
            //�g���[�h�c�̐V�K�̔�
            // �悸�́A�V���A���̍\���v�f�̃p�^�[�����K�����A���[�U�[�f�X�N�g�b�v�̐ݒ�t�@�C�����g�p���Ċm�F����
            string matchResult = string.Empty;
            //string matchResult = matchSerialNumberingPattern(module);
            if (matchResult != string.Empty)
            { 
                MessageBox.Show(matchResult + " does not match with desktop file's setting.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ��������̃e�[�u���Ƀ��R�[�h��ǉ�
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
            // log = Environment.NewLine + earlyTime + "," + module + "," + displayAll;

            // �������t�̃t�@�C�������݂���ꍇ�͒ǋL���A���݂��Ȃ��ꍇ�̓t�@�C�����쐬�ǋL����
            try
            {
                string outFile = outPath + DateTime.Today.ToString("yyyyMMdd") + ".txt";
                System.IO.File.AppendAllText(outFile, log, System.Text.Encoding.GetEncoding("UTF-8"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // �f�[�^�O���b�g�r���[�̍X�V
            updateDataGridViews(dtModule, ref dgvModule);
        }

        // �T�u�v���V�[�W���F�V���A���̍\���v�f�̃p�^�[�����K�����A���[�U�[�f�X�N�g�b�v�̐ݒ�t�@�C�����g�p���Ċm�F����
        private string matchSerialNumberingPattern(string serial)
        {
            string result = string.Empty;

            // �ݒ�t�@�C�� �k�h�m�d �� �w�w�w �̏ꍇ�́A���؂��Ȃ�
            // if (line == "XXX") return string.Empty;
            if      (!plant.Equals("XXX")      && VBS.Mid(serial,  1, 3) != plant)        return "Plant '"       + VBS.Mid(serial,  1, 3) + "'";
            else if (!year.Equals("XXX")       && VBS.Mid(serial,  4, 1) != year)         return "Year '"        + VBS.Mid(serial,  4, 1) + "'";
            else if (!week.Equals("XXX")       && VBS.Mid(serial,  5, 2) != week)         return "Week '"        + VBS.Mid(serial,  5, 2) + "'";
            else if (!day.Equals("XXX")        && VBS.Mid(serial,  7, 1) != day)          return "Day '"         + VBS.Mid(serial,  7, 1) + "'";
            else if (!line.Equals("XXX")       && VBS.Mid(serial,  8, 1) != line)         return "Line '"        + VBS.Mid(serial,  8, 1) + "'";
            else if (!eeee.Equals("XXX")       && VBS.Mid(serial, 12, 4) != eeee)         return "4E '"          + VBS.Mid(serial, 12, 4) + "'";
            else if (!revision.Equals("XXX")   && VBS.Mid(serial, 16, 1) != revision)     return "Revision '"    + VBS.Mid(serial, 16, 1) + "'";
            else if (!mass.Equals("XXX")       && VBS.Mid(serial, 19, 1) != mass)         return "Mass '"        + VBS.Mid(serial, 19, 1) + "'";
            else if (!flexure.Equals("XXX")    && VBS.Mid(serial, 20, 1) != flexure)      return "Flexure '"     + VBS.Mid(serial, 20, 1) + "'";
            else if (!cover_base.Equals("XXX") && VBS.Mid(serial, 21, 1) != cover_base)   return "Cover/base '"  + VBS.Mid(serial, 21, 1) + "'";
            else if (!dframe.Equals("XXX")     && VBS.Mid(serial, 22, 1) != dframe)       return "D-Frame '"     + VBS.Mid(serial, 22, 1) + "'";
            else if (!fpc.Equals("XXX")        && VBS.Mid(serial, 23, 1) != fpc)          return "FPC '"         + VBS.Mid(serial, 23, 1) + "'";
            else if (!shift.Equals("XXX")      && VBS.Mid(serial, 24, 1) != shift)        return "Shift '"       + VBS.Mid(serial, 24, 1) + "'";
            else return string.Empty;
        }

        // �r���[���[�h�ōĈ�����s��
        private void btnPrint_Click(object sender, EventArgs e)
        {
            TfSato tfs = new TfSato();
            tfs.printStart("tray", txtTrayId.Text, dtLot, string.Empty, dtpRegisterDate.Value, string.Empty, string.Empty, 1, string.Empty);
        }

        // �o�^�{�^���������A�e��m�F�A�{�b�N�X�h�c�̔��s�A�V���A���̓o�^�A�o�[�R�[�h���x���̃v�����g���s��
        private void btnRegisterTray_Click(object sender, EventArgs e)
        {
            if (cmbBinShift.Text == string.Empty)
            {
                MessageBox.Show("Please select Shift.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }
            if (getOkCount(dtModule) != dtModule.Rows.Count)
            {
                MessageBox.Show("Module is not 24.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            // �o�^�������́A����{�^���������A���ׂẴR���g���[���𖳌��ɂ���
            cmbBinShift.Enabled = false;
            txtModuleId.Enabled = false;
            btnRegisterTray.Enabled = false;
            btnDeleteSelection.Enabled = false;
            btnChangeCapacity.Enabled = false;

            if (formRefillMode && countBeforeRefill >= dtModule.Rows.Count)
            {
                MessageBox.Show("Refill is not correct.  Please close this window.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                return;
            }

            //�ꎞ�e�[�u���̃V���A���S�Ăɂ��āA�c�a�e�[�u���Ɋ��ɓo�^���Ȃ����A�m�F����
            //�i�q�d�e�h�k�k���[�h�̏ꍇ�́A�ǉ����̃��W���[���̂݊m�F����j
            DataTable dtTarget = new DataTable();
            if (formRefillMode) dtTarget = subtractTable(dtModule, dtModuleBeforeRefill);
            else dtTarget = dtModule;

            TfSQL tf = new TfSQL();
            string dbDuplicate = tf.sqlModuleDuplicateCheck(dtTarget);
            if (dbDuplicate != string.Empty)
            {
                for (int i = 0; i < dgvModule.Rows.Count; ++i)
                { 
                    if (dgvModule["module_id", i].Value.ToString() == dbDuplicate)
                        dgvModule["module_id", i].Style.BackColor = Color.Red;
                }
                soundAlarm();

                // �q�d�e�h�k�k���[�h�̏ꍇ�́A����ȊO�̑I������^���Ȃ�
                if (!formRefillMode)
                {
                    btnDeleteSelection.Enabled = true;
                }
                return;
            }

            // �[�U���[�h���ۂ��ŁA�Ăяo���v���V�[�W����I�����Ď��s����
            if (!formRefillMode) registerModuleNormalMode();
            else registerModuleRefillMode();
        }

        // �T�u�v���V�[�W���F�q�d�e�h�k�k���[�h�̐V�K�ǉ����݂̂��擾����
        private DataTable subtractTable(DataTable dtAll, DataTable dtSub)
        {
            DataTable dtAfter = (from a in dtAll.AsEnumerable()
                                 join s in dtSub.AsEnumerable()
                                 on a["module_id"].ToString() equals s["module_id"].ToString()
                                 into g
                                 where g.Count() == 0
                                 select a).CopyToDataTable();
            return dtAfter;
        }

        // �T�u�v���V�[�W���F���W���[���̓o�^�A�ʏ탂�[�h
        private void registerModuleNormalMode()
        {
            // 2016.08.10 FUJII  �g���[�h�c�̐V�K�̔ԃv���V�[�W���uGetNewTrayId�v���A�g�����U�N�V���������o�[�W�����֕ύX
            //�g���[�h�c�̐V�K�̔�
            //string trayNew = getNewTrayId(txtLoginDept.Text, txtLoginName.Text, VBS.Mid(maxLot, 8, 1), cmbBinShift.Text);
            TfSQL tf = new TfSQL();
            string trayNew = tf.sqlGetNewTrayId(txtLoginDept.Text, txtLoginName.Text, VBS.Mid(maxLot, 8, 1), cmbBinShift.Text, maxLot, dtLot, ref registerDate);
            if (trayNew == string.Empty)
            {
                MessageBox.Show("An error happened in the tray id issuing process.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            //���W���[���e�[�u���ցA�f�[�^�e�[�u���̃��R�[�h���ꊇ�o�^����
            bool res = tf.sqlMultipleInsertModule(dtModule, trayNew);

            if (res)
            {
                //���x���̃v�����g�A�E�g
                TfSato tfs = new TfSato();
                tfs.printStart("tray", trayNew, dtLot, string.Empty, dtpRegisterDate.Value, string.Empty, string.Empty, 1, string.Empty);

                //�o�^�ς݂̏�Ԃ�\��
                txtTrayId.Text = trayNew;
                dtpRegisterDate.Value = registerDate;

                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
                this.Focus();
                MessageBox.Show("Tray ID: " + trayNew + Environment.NewLine +
                    "and its modules were registered.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //���[�U�[�ɂ�郁�b�Z�[�W�{�b�N�X�m�F��̏���
                txtTrayId.Text = String.Empty;
                txtModuleId.Text = String.Empty;
                dtModule.Clear();
                capacity = 24;
                updateDataGridViews(dtModule, ref dgvModule);
            }
        }

        // �T�u�v���V�[�W���F���W���[���̓o�^�A�[�U���[�h
        private void registerModuleRefillMode()
        {
            string trayCurrent = txtTrayId.Text;

            //���W���[���e�[�u���̕�[�O���R�[�h����U�폜���A�g���[�e�[�u���̍X�V������ǉ�����
            bool res1 = updateTrayIdAndDeleteModule(trayCurrent, txtLoginDept.Text, txtLoginName.Text);
            if (!res1)
            {
                MessageBox.Show("An error happened in the tray id updating process.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            //���W���[���e�[�u���ցA�f�[�^�e�[�u���̃��R�[�h���ꊇ�o�^����
            TfSQL tf = new TfSQL();
            bool res2 = tf.sqlMultipleInsertModule(dtModule, trayCurrent);

            if (res2)
            {
                //���x���̃v�����g�A�E�g
                TfSato tfs = new TfSato();
                tfs.printStart("tray", txtTrayId.Text, dtLot, string.Empty, dtpRegisterDate.Value, string.Empty, string.Empty, 1, string.Empty);

                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
                this.Focus();
                MessageBox.Show("Tray ID: " + trayCurrent + Environment.NewLine +
                    "and its modules were updated.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //���[�U�[�ɂ�郁�b�Z�[�W�{�b�N�X�m�F��̏���
                formAddMode = false;
                formRefillMode = false;
                updateDataGridViews(dtModule, ref dgvModule);

                //����{�^���ݗL���Ƃ��āA���邱�Ƃ𑣂�
                cmbBinShift.Enabled = false;
                txtModuleId.Enabled = false;
                btnRegisterTray.Enabled = false;
                btnDeleteSelection.Enabled = false;
                btnChangeCapacity.Enabled = false;
            }
        }

        // �T�u�v���V�[�W���F�g���[���̍X�V�i���W���[�����폜�����t���j
        private bool updateTrayIdAndDeleteModule(string trayId, string udept, string uname)
        {
            // ���W���[�����ʁA�������b�g�敪�A�X�V���A�X�V�����A�X�V���[�U�[�ɂ��čX�V����
            int qty = (int)dtLot.Rows[0]["total"];
            string multiLot = dtLot.Columns.Count >= 3 ? "T" : "F";
            string sql = "update t_tray set " +
                "qty ='" + qty + "', " +
                "update_date ='" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "', " +
                "up_dept ='" + udept + "', " +
                "up_user ='" + uname + "', " +
                "multi_lot ='" + multiLot + "' " +
                "where tray_id ='" + trayId + "'";

            // ���W���[�����R�[�h�́A���L�T�u�v���V�[�W�����ň�U�L�����Z�����A�ʃv���V�[�W���ňꊇ�o�^�j
            System.Diagnostics.Debug.Print(sql);
            TfSQL tf = new TfSQL();
            bool res = tf.sqlUpdateModuleInPack(trayId, sql);
            return res;
        }

        // �T�u�v���V�[�W���F�g���[���̍X�V
        private bool updateTrayId(string trayId, string udept, string uname)
        {
            // ���W���[�����ʁA�������b�g�敪�A�X�V���A�X�V�����A�X�V���[�U�[�ɂ��čX�V����
            int qty = (int)dtLot.Rows[0]["total"];
            string multiLot = dtLot.Columns.Count >= 3 ? "T" : "F";
            string sql = "update t_tray set " +
                "qty ='" + qty + "', " +
                "update_date ='" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "', " +
                "up_dept ='" + udept + "', " +
                "up_user ='" + uname + "', " +
                "multi_lot ='" + multiLot + "' " +
                "where tray_id ='" + trayId + "'";

            System.Diagnostics.Debug.Print(sql);
            TfSQL tf = new TfSQL();
            bool res = tf.sqlExecuteNonQuery(sql, false);
            return res;
        }

        // �ꎞ�e�[�u���̑I�����ꂽ�������R�[�h���A�ꊇ����������
        private void btnDeleteSelection_Click(object sender, EventArgs e)
        {
            if (dtModule.Rows.Count <= 0) return;

            // �Z���̑I��͈͂��Q��ȏ�̏ꍇ�́A���b�Z�[�W�̕\���݂̂Ńv���V�[�W���𔲂���
            if (dgvModule.Columns.GetColumnCount(DataGridViewElementStates.Selected) >= 2)
            {
                MessageBox.Show("Please select range with only one column.", "Notice",�@MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }

            // �����L�����Z�����[�h�łȂ����́A�ʏ폈���i���W���[���f�[�^�e�[�u���̑I���s�폜�j
            if (!formPartialCancelMode)
            {
                DialogResult result = MessageBox.Show("Do you really want to delete the selected rows?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No) return;

                foreach (DataGridViewCell cell in dgvModule.SelectedCells)
                {
                    dtModule.Rows[cell.RowIndex].Delete();
                }
                dtModule.AcceptChanges();
                updateDataGridViews(dtModule, ref dgvModule);
                txtModuleId.Focus();
                txtModuleId.SelectAll();
            }

            // �����L�����Z�����[�h�̎��́A�c�a���W���[���e�[�u���̍s�폜
            else if (formPartialCancelMode)
            {
                if (dgvModule.SelectedCells.Count == dtModule.Rows.Count)
                {
                    MessageBox.Show("If you wish to delete all modules, please cancel the tray.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                    return;
                }

                DialogResult result = MessageBox.Show("Do you really want to delete the selected rows?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No) return;

                btnDeleteSelection.Enabled = false;
                string sql1 = "delete from t_module where module_id in ('";
                string sql2 = string.Empty;
                foreach (DataGridViewCell cell in dgvModule.SelectedCells)
                {
                    sql2 += dtModule.Rows[cell.RowIndex]["module_id"].ToString() + "','";
                    dtModule.Rows[cell.RowIndex].Delete();
                }
                string sql3 = sql1 + VBS.Left(sql2, sql2.Length - 2) + ")";
                TfSQL tf = new TfSQL();
                bool res = tf.sqlExecuteNonQuery(sql3, false);

                if (res)
                {
                    dtModule.AcceptChanges();
                    updateDataGridViews(dtModule, ref dgvModule);

                    string updateTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    string sql4 = "update t_tray set update_date = '" + updateTime + "', up_dept = '" + txtLoginDept.Text + "', " + 
                        "up_user = '" + txtLoginName.Text + "', lot = '" + dtLot.Columns[0].ColumnName + "', " +
                        "qty = '" + dtLot.Rows[0]["total"].ToString() + "', multi_lot = '" + (dtLot.Columns.Count >= 3 ? "T" : "F") + "' " +
                        "where tray_id = '" + txtTrayId.Text + "'";
                    System.Diagnostics.Debug.Print(sql4);
                    tf.sqlExecuteNonQuery(sql4, false);

                    //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                    this.RefreshEvent(this, new EventArgs());
                    this.Focus();
                    MessageBox.Show("Partial cancel was successful.", "Result", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                }
                else
                {
                    MessageBox.Show("Partial cancel failed.", "Result", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                }
            }
        }

        // �P�g���[������̃��W���[������ύX����i�Ǘ��������[�U�[�̂݁j�A�{�^���̃N���b�N����Ăяo��
        private void btnChangecapacity_Click(object sender, EventArgs e)
        {
            //if (userRole == "super" && txtLoginDept.Text == "PC")
            if (userRole == "super")
                changeCapacity(sender, e);
        }

        // �P�g���[������̃��W���[������ύX����i�Ǘ��������[�U�[�̂݁j�A�e�L�X�g�{�b�N�X�̃_�u���N���b�N����Ăяo��
        private void txtOkCount_DoubleClick(object sender, EventArgs e)
        {
            //if (userRole == "super" && txtLoginDept.Text == "PC")
            if (userRole == "super")
                changeCapacity(sender, e);
        }

        // �T�u�v���V�[�W���F�P�g���[������̃��W���[������ύX
        private void changeCapacity(object sender, EventArgs e)
        {
            //2016/08/05 �d�l�ύX(24�ȊO�͈�ؔF�߂Ȃ�)
            //2016/09/30 �d�l�ύX(�q�ɃX�[�p�[���[�U�[�̂݁A�ύX�\)
            //return;

            // ���ɓ��t�H�[�����J����Ă���ꍇ�́A�������s��Ȃ�
            if (TfGeneral.checkOpenFormExists("frmCapacity"))
            {
                MessageBox.Show("Please close or complete another form.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }

            frmCapacity fC = new frmCapacity();
            //�q�C�x���g���L���b�`���āA�f�[�^�O���b�h���X�V����
            fC.RefreshEvent += delegate (object sndr, EventArgs excp)
            {
                capacity = fC.returnCapacity();
                updateDataGridViews(dtModule, ref dgvModule);
            };

            fC.updateControls(capacity);
            fC.Show();
        }

        // �X�[�p�[���[�U�[�Ɍ���A�o�^�ς݂̃V���A���̒u���������ł���i�p�b�N��͕s�j
        private void btnReplace_Click(object sender, EventArgs e)
        {
            if (dtModule.Rows.Count <= 0) return;

            // �Z���̑I��͈͂��Q��ȏ�̏ꍇ�́A���b�Z�[�W�̕\���݂̂Ńv���V�[�W���𔲂���
            if (dgvModule.Columns.GetColumnCount(DataGridViewElementStates.Selected) >= 2 ||
                    dgvModule.Rows.GetRowCount(DataGridViewElementStates.Selected) >= 2 ||
                    dgvModule.CurrentCell.ColumnIndex != 0)
            {
                MessageBox.Show("Please select only one serial number.", "Notice",�@MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }

            // �u�������p�t�H�[�������ɊJ���Ă��Ȃ����A�m�F����
            if (TfGeneral.checkOpenFormExists("frmModuleReplace"))
            {
                MessageBox.Show("Please close or complete another form.", "Notice",�@MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }

            string curSerial = dgvModule.CurrentCell.Value.ToString();
            int curRowIndex = dgvModule.CurrentRow.Index;
            frmModuleReplace fR = new frmModuleReplace();

            //�u�������t�H�[���C�x���g���L���b�`���āA���t�H�[���f�[�^�O���b�h���X�V���A�g���[�e�[�u�����X�V����
            fR.RefreshEvent += delegate(object sndr, EventArgs excp)
            {
                //���W���[���e�[�u���E�f�[�^�O���b�h�̍X�V
                readModuleInfo(ref dtModule);
                updateDataGridViews(dtModule, ref dgvModule);
                //�g���[�e�[�u���̍X�V�A����ɁA�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                updateTrayId(txtTrayId.Text, txtLoginDept.Text, txtLoginName.Text);
                this.RefreshEvent(this, new EventArgs());
                this.Focus();
            };

            fR.updateControls(txtTrayId.Text, curSerial, curRowIndex +1, formReturnMode, cmbBinShift.Text);
            fR.Show();
        }

        // �X�[�p�[���[�U�[�Ɍ���A�o�^�ς݃g���[���L�����Z���ł���i�p�b�N��͕s�j
        private void btnCancelTray_Click(object sender, EventArgs e)
        {
            // �{���ɍ폜���Ă悢���A�Q�d�Ŋm�F����B
            DialogResult result1 = MessageBox.Show("Do you really want to cancel this tray?",�@"Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result1 == DialogResult.No) return;

            DialogResult result2 = MessageBox.Show("Are you really sure? Please select NO if you are not sure.", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result2 == DialogResult.No) return;

            // �L�����Z���̎��s
            string trayId = txtTrayId.Text;
            TfSQL tf = new TfSQL();
            bool res = tf.sqlCancelModuleInTray(trayId, txtLoginDept.Text, txtLoginName.Text);
            if (res)
            {
                //�{�t�H�[���̃f�[�^�O���b�g�r���[�X�V
                dtModule.Clear();
                updateDataGridViews(dtModule, ref dgvModule);

                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
                this.Focus();
                MessageBox.Show("Tray ID " + trayId + " and its modules were canceled.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // ���b�Z�[�W�{�b�N�X�̊m�F��A����
                Close();
            }
            else
            {
                MessageBox.Show("Cancel process was not successful.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ���t�H�[���̃N���[�Y���A���̎q�t�H�[�����J���Ă��Ȃ����Ƃ��m�F����
        private void btnCancel_Click(object sender, EventArgs e)
        {
            // frmCapacity ����Ă��Ȃ��ꍇ�́A��ɕ���悤�ʒm����
            if (TfGeneral.checkOpenFormExists("frmCapacity"))
            {
                MessageBox.Show("You need to close Capacity form before canceling.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                return;
            }
            // frmModuleReplace ����Ă��Ȃ��ꍇ�́A��ɕ���悤�ʒm����
            if (TfGeneral.checkOpenFormExists("frmModuleReplace"))
            {
                MessageBox.Show("You need to close Replace form before canceling..", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                return;
            }

            Close();
        }

        // �T�u�v���V�[�W���F�e�X�g���ʂ��e�`�h�k�܂��̓��R�[�h�Ȃ��̃V���A�����}�[�L���O����
        private void colorViewForFailAndBlank(ref DataGridView dgv)
        {
            for (int i = 0; i < dgv.Rows.Count; ++i)
            {
                if (dgv["test_result", i].Value.ToString() == "FAIL" || dgv["test_result", i].Value.ToString() == "NG" || dgv["test_result", i].Value.ToString() == string.Empty)
                {
                    dgv["tester_id", i].Style.BackColor = Color.Red;
                    dgv["test_result", i].Style.BackColor = Color.Red;
                    dgv["test_date", i].Style.BackColor = Color.Red;
                    soundAlarm();
                }
                else
                {
                    dgv["tester_id", i].Style.BackColor = Color.FromKnownColor(KnownColor.Window);
                    dgv["test_result", i].Style.BackColor = Color.FromKnownColor(KnownColor.Window);
                    dgv["test_date", i].Style.BackColor = Color.FromKnownColor(KnownColor.Window);
                }
            }
        }

        // �T�u�v���V�[�W���F�d�����R�[�h�A�܂��͂P�Z���Q�d���͂��}�[�L���O����
        private void colorViewForDuplicateSerial(ref DataGridView dgv)
        {
            if (dgv.Rows.Count <= 0) return;

            DataTable dt = ((DataTable)dgv.DataSource).Copy();
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                string module = dgv["module_id", i].Value.ToString();
                DataRow[] dr = dt.Select("module_id = '" + module + "'");
                if (dr.Length >= 2)
                {
                    dgv["module_id", i].Style.BackColor = Color.Red;
                    soundAlarm();
                }
                else
                {
                    dgv["module_id", i].Style.BackColor = Color.FromKnownColor(KnownColor.Window);
                }
            }
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

        // �f�[�^�O���b�h�r���[�̃_�u���N���b�N���A�f�[�^���G�N�Z���փG�N�X�|�[�g
        private void dgvOverall_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            DataTable dt = new DataTable();
            dt = (DataTable)dgvModule.DataSource;
            ExcelClass xl = new ExcelClass();
            xl.ExportToExcel(dt);
        }

        // �k�n�s�W�v�O���b�h�r���[���̃{�^�����������A�Ώۂ̃��W���[�����G�N�Z���t�@�C���֏o�͂���
        private void btnExportModule_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();
            dt = (DataTable)dgvModule.DataSource;
            ExcelClass xl = new ExcelClass();
            // 2016.08.29 FUJII �G�N�Z���ւ̏o�͂���A�f�X�N�g�b�v�b�r�u�ւ̏o�͂֕ύX
            xl.ExportToCsv(dt, System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\tray.csv");
        }

        // �K�萔�ʂɒB���Ă��Ȃ��g���C�ɂ��āA���W���[����ǉ�����i�p�b�N��͕s�j
        private void btnRefillTray_Click(object sender, EventArgs e)
        {
            formAddMode = true;
            formRefillMode = true;
            changeMode();
            dtModuleBeforeRefill = dtModule.Copy();
            countBeforeRefill = dtModule.Rows.Count;
        }

        // �a�h�m�E�V�t�g���A�}�X�^�[�Ŏw�肷��
        private void cmbShift_KeyDown(object sender, KeyEventArgs e)
        {
            // �X�[�p�[���[�U�[���g�p���̏ꍇ�̂݁A���[�U�[�}�X�^�[�ύX�t�H�[�����J��
            if (formPartialCancelMode) return;
            if (e.KeyCode != Keys.Enter || userRole != "super") return;
            if (TfGeneral.checkOpenFormExists("frmMasterCriteria")) return;
            frmMasterCriteria fI = new frmMasterCriteria("BIN_SHIFT");
            //�q�C�x���g���L���b�`���āA�f�[�^�O���b�h���X�V����
            fI.RefreshEvent += delegate (object sndr, EventArgs excp)
            {
                //�o�b�`�R���{�{�b�N�X�̐ݒ�
                setShiftComboBox();
            };
            fI.Show();
        }

        // �e�e��̗e�ʂ��A�}�X�^�[�Ŏw�肷��
        private void txtOkCount_KeyDown(object sender, KeyEventArgs e)
        {
            // �X�[�p�[���[�U�[���g�p���̏ꍇ�̂݁A���[�U�[�}�X�^�[�ύX�t�H�[�����J��
            if (e.KeyCode != Keys.Enter || userRole != "super") return;
            if (TfGeneral.checkOpenFormExists("frmMasterCriteria")) return;
            frmMasterCriteria fI = new frmMasterCriteria("BIN_SHIFT");
            //�q�C�x���g���L���b�`���āA�f�[�^�O���b�h���X�V����
            fI.RefreshEvent += delegate (object sndr, EventArgs excp)
            {
                //�o�b�`�R���{�{�b�N�X�̐ݒ�
                setShiftComboBox();
            };
            fI.Show();
        }

        // �f�[�^�O���b�h�̃��W���[���h�c����������
        private void frmModuleInTray_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // ���ɓ��t�H�[�����J����Ă���ꍇ�́A�������s��Ȃ�
            if (TfGeneral.checkOpenFormExists("frmModuleFind"))
            {
                MessageBox.Show("Please close or complete another form.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }

            frmModuleFind fC = new frmModuleFind();
            //�q�C�x���g���L���b�`���āA�Y������Z���Ƀt�H�[�J�X����
            fC.RefreshEvent += delegate (object sndr, EventArgs excp)
            {
                string target = fC.returnTargetModule();
                for (int i = 0; i < dtModule.Rows.Count; i++)
                {
                    if (dtModule.Rows[i]["module_id"].ToString() == target) dgvModule.CurrentCell = dgvModule.Rows[i].Cells[0];
                }
            };
      
            fC.Show();
        }

        // ����{�^����V���[�g�J�b�g�ł̏I���������Ȃ�
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x112;
            const long SC_CLOSE = 0xF060L;
            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt64() & 0xFFF0L) == SC_CLOSE) { return; }
            base.WndProc(ref m);
        }
    }
}