using System;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TrayGuard
{
    public partial class frmTrayInPack : Form
    {
        //�e�t�H�[��frmTray�փC�x���g������A���i�f���Q�[�g�j
        public delegate void RefreshEventHandler(object sender, EventArgs e);
        public event RefreshEventHandler RefreshEvent;

        //�f�[�^�O���b�h�r���[�p�{�^��
        DataGridViewButtonColumn openTray;

        // �v�����g�p�e�L�X�g�t�@�C���̕ۑ��p�t�H���_���A��{�ݒ�t�@�C���Őݒ肷��
        string appconfig = System.Environment.CurrentDirectory + "\\info.ini";
        string productconfig = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\tray_guard_desktop_for_pack.ini";

        //���̑��A�񃍁[�J���ϐ�
        DataTable dtTray;
        DataTable dtLot;
        bool formAddMode;
        bool formReprintMode;
        bool packIdCanceled;
        bool packIdCartoned;
        string userRole;
        int okCount;
        int capacity = 13;
        string maxLot;
        DateTime registerDate;
        bool sound;
        int position;

        // ���i�V���A���\���v�f�`�F�b�N�p�ϐ�
        List<string> plant1;
        List<string> year1;
        List<string> week1;
        List<string> day1;
        List<string> line1;
        List<string> eeee1;
        List<string> revision1;
        List<string> mass1;
        List<string> flexure1;
        List<string> cover_base1;
        List<string> dframe1;
        List<string> fpc1;
        List<string> shift1;

        // �R���X�g���N�^
        public frmTrayInPack()
        {
            InitializeComponent();

            // ���i�V���A���\���v�f�́A�ϐ��ւ̊i�[
            plant1 = readIni("MODULE NUMBERING CHECK FOR PACK", "PLANT", productconfig).Split(',').ToList();
            year1 = readIni("MODULE NUMBERING CHECK FOR PACK", "YEAR", productconfig).Split(',').ToList();
            week1 = readIni("MODULE NUMBERING CHECK FOR PACK", "WEEK", productconfig).Split(',').ToList();
            day1 = readIni("MODULE NUMBERING CHECK FOR PACK", "DAY", productconfig).Split(',').ToList();
            line1 = readIni("MODULE NUMBERING CHECK FOR PACK", "LINE", productconfig).Split(',').ToList();
            eeee1 = readIni("MODULE NUMBERING CHECK FOR PACK", "EEEE", productconfig).Split(',').ToList();
            revision1 = readIni("MODULE NUMBERING CHECK FOR PACK", "REVISION", productconfig).Split(',').ToList();
            mass1 = readIni("MODULE NUMBERING CHECK FOR PACK", "MASS", productconfig).Split(',').ToList();
            flexure1 = readIni("MODULE NUMBERING CHECK FOR PACK", "FLEXURE", productconfig).Split(',').ToList();
            cover_base1 = readIni("MODULE NUMBERING CHECK FOR PACK", "COVER/BASE", productconfig).Split(',').ToList();
            dframe1 = readIni("MODULE NUMBERING CHECK FOR PACK", "D-FRAME", productconfig).Split(',').ToList();
            fpc1 = readIni("MODULE NUMBERING CHECK FOR PACK", "FPC", productconfig).Split(',').ToList();
            shift1 = readIni("MODULE NUMBERING CHECK FOR PACK", "SHIFT", productconfig).Split(',').ToList();
        }

        // ���[�h���̏���
        private void frmTrayInPack_Load(object sender, EventArgs e)
        {
            this.Text = this.Text + " " + Assembly.GetExecutingAssembly().GetName().Version;
            // ���t�H�[���̕\���ꏊ���w��
            this.Left = 250;
            this.Top = 20;
            if (position == 2) { this.Left = 400; this.Top = 30; }
            else if (position == 3) { this.Left = 450; this.Top = 40; }
            else if (position == 4) { this.Left = 500; this.Top = 50; }

            // �e�폈���p�̃e�[�u���𐶐����A�f�[�^��ǂݍ���
            dtTray = new DataTable();
            defineTrayTable(ref dtTray);
            if (!formAddMode) readTrayInfo(ref dtTray);

            // �O���b�g�r���[�̍X�V
            updateDataGridViews(dtTray, ref dgvTray, true);

            // �ǉ����[�h�A�{�����[�h�̐؂�ւ�
            changeMode();

            // �T�u�v���V�[�W���F �o�b�`�R���{�{�b�N�X�̐ݒ�
            setBatchComboBox();
        }

        // �T�u�v���V�[�W���F �o�b�`�R���{�{�b�N�X�̐ݒ�
        private void setBatchComboBox()
        {
            string sql = "select content from t_criteria where criteria = 'BATCH' order by content"; 
            TfSQL tf = new TfSQL();
            tf.getComboBoxData(sql, ref cmbBatch);
        }

        // �T�u�v���V�[�W���F �ǉ����[�h�A�{�����[�h�̐؂�ւ�
        private void changeMode()
        {
            // �ǉ����[�h�̏ꍇ
            if (formAddMode)
            {
                cmbBatch.Enabled = false; 
                txtTrayId.Enabled = true;
                btnRegisterPack.Text = "Register Pack";
                btnRegisterPack.Enabled = false;
                btnCancelPack.Enabled = false;
                btnClose.Enabled = true;
                btnDeleteSelection.Enabled = true;
            }
            // �{�����[�h�̏ꍇ
            else
            {
                cmbBatch.Enabled = false;
                txtTrayId.Enabled = false;
                btnRegisterPack.Text = "Re-Print Label";
                // 2016.09.24 FUJII  �ăv�����g���[�h��ǉ��ifrmPack�ɂāA�g���[�h�c���L�[�Ƃ��āA�p�b�N�h�c����肵���ꍇ�̂݁A�Ĉ���\�j
                //btnRegisterPack.Enabled = packIdCanceled ? false : ((formReprintMode && userRole == "super") ? true : false);
                //btnRegisterPack.Enabled = packIdCanceled ? false : (userRole == "super" ? true : false);
                // 2017.01.18 FUJII  ���[�U�[��������
                btnRegisterPack.Enabled = packIdCanceled ? false : true;

                btnCancelPack.Enabled = false;
                btnClose.Enabled = true;
                btnDeleteSelection.Enabled = false;
                if (userRole == "super") btnCancelPack.Enabled = (packIdCanceled || packIdCartoned) ? false : true;
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
            bool addMode, bool returnMode, bool canceled, bool cartoned, int pos, bool reprintMode)
        {
            txtPackId.Text = tid;
            dtpRegisterDate.Value = tdate;
            txtLoginName.Text = uname;
            txtLoginDept.Text = udept;
            userRole = urole;
            if (!addMode) cmbBatch.Text = shift;
            formAddMode = addMode;
            packIdCanceled = canceled;
            packIdCartoned = cartoned;
            position = pos;
            formReprintMode = reprintMode;
        }

        // �T�u�v���V�[�W���F�f�[�^�e�[�u���̒�`
        private void defineTrayTable(ref DataTable dt)
        {
            dt.Columns.Add("tray_id", typeof(string));
            dt.Columns.Add("lot", typeof(string));
            dt.Columns.Add("qty", typeof(int));
            dt.Columns.Add("register_date", typeof(DateTime));
            dt.Columns.Add("rg_dept", typeof(string));
            dt.Columns.Add("multi_lot", typeof(string));
            dt.Columns.Add("check", typeof(string));
        }

        // �T�u�v���V�[�W���F�c�a����f�[�^�e�[�u���ւ̓ǂݍ���
        private void readTrayInfo(ref DataTable dt)
        {
            dt.Rows.Clear();
            string sql = "select tray_id, lot, qty, register_date, rg_dept, multi_lot, " +
                "'OK' as check from t_tray where pack_id='" + txtPackId.Text + "'";
            TfSQL tf = new TfSQL();
            System.Diagnostics.Debug.Print(sql);
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql, ref dt);
        }


        // �T�u�v���V�[�W���F�f�[�^�O���b�g�r���[�̍X�V
        private void updateDataGridViews(DataTable dt1, ref DataGridView dgv1, bool load)
        {
            // �f�[�^�O���b�g�r���[�ւc�s�`�`�s�`�a�k�d���i�[
            dgv1.DataSource = dt1;
            dgv1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            // �e�X�g���ʂ��e�`�h�k�܂��̓��R�[�h�Ȃ��̃V���A�����}�[�L���O���� 
            if(formAddMode) colorViewForCanceledAndPacked(ref dgv1);

            // �d�����R�[�h�A����тP�Z���Q�d���͂��}�[�L���O����
            if (formAddMode) colorViewForDuplicateTray(ref dgv1);

            // �O���b�g�r���[�E�[�Ƀ{�^����ǉ��i����̂݁j
            if (load && !formAddMode) addButtonsToDataGridView(dgv1);

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
                    cmbBatch.Enabled = false;
                    txtTrayId.Enabled = false;
                    btnRegisterPack.Enabled = true;
                    btnDeleteSelection.Enabled = true;
                }
                else
                {
                    cmbBatch.Enabled = false;
                    txtTrayId.Enabled = true;
                    btnRegisterPack.Enabled = false;
                    btnDeleteSelection.Enabled = true;
                    txtTrayId.SelectAll(); // �A���X�L�����p
                }
            }
        }

        // �T�u�T�u�v���V�[�W���F�O���b�g�r���[�E�[�Ƀ{�^����ǉ�
        private void addButtonsToDataGridView(DataGridView dgv)
        {
            openTray = new DataGridViewButtonColumn();
            openTray.HeaderText = string.Empty;
            openTray.Text = "Open";
            openTray.UseColumnTextForButtonValue = true;
            openTray.Width = 80;
            dgv.Columns.Add(openTray);
        }

        // �T�u�v���V�[�W���F�V���A���ԍ��d���Ȃ��̂o�`�r�r�����擾����
        private int getOkCount(DataTable dt)
        {
            DataTable distinct = dt.DefaultView.ToTable(true, new string[] { "tray_id", "check" });
            DataRow[] dr = distinct.Select("check = 'OK'");
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

            // �ꎞ�e�[�u���ɁA���b�g�W�v���i�[����
            string sql1 = "select lot, count(lot) as qty from t_module where tray_id in (";
            string sql2 = string.Empty;
            var query1 = dt.AsEnumerable()
                        .Select(row => new { tray_id = row.Field<string>("tray_id") });
            foreach (var q in query1) sql2 += "'" + q.tray_id + "', ";
            string sql3 = sql1 + VBS.Left(sql2, sql2.Length - 2) + ") group by lot order by qty desc, lot";
            System.Diagnostics.Debug.Print(sql3);
            DataTable dtTemp = new DataTable();
            TfSQL tf = new TfSQL();
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql3, ref dtTemp);

            // ���b�g�W�v�\���e�[�u���ɁA�ꎞ�e�[�u���̏����ڂ�
            dgvLotSummary.DataSource = null;
            dgvLotSummary.Refresh();
            dtLot = new DataTable();
            var query2 = dtTemp.AsEnumerable().Select(r => new {lot = r.Field<string>("lot"), qty = r.Field<Int64>("qty") });
            // ��̒ǉ�
            foreach (var q in query2) dtLot.Columns.Add(q.lot, typeof(int));
            dtLot.Columns.Add("total", typeof(int));
            // �s�̒ǉ�
            dtLot.Rows.Add(); 
            foreach (var q in query2) dtLot.Rows[0][q.lot] = q.qty;
            dtLot.Rows[0]["total"] = query2.Sum(a => a.qty);
 
            dgvLotSummary.DataSource = dtLot;
            return query2.First().lot;
        }

        // �V���A�����X�L�������ꂽ���̏���            
        private void txtModuleId_KeyDown(object sender, KeyEventArgs e)
        {
            // �G���^�[�L�[�̏ꍇ�A�e�L�X�g�{�b�N�X�̌������P�T���̏ꍇ�̂݁A�������s��
            if (e.KeyCode != Keys.Enter || txtTrayId.Text.Length != 21) return;

            string tray = txtTrayId.Text;
            string sql = "select tray_id, lot, qty, register_date, rg_dept, multi_lot, " +
                "case when cancel_date is not null then to_char(cancel_date,'YYYY/MM/DD') " +
                "when pack_id is not null then pack_id else 'OK' end as check " +
                "from t_tray where tray_id='" + tray + "'";
            System.Diagnostics.Debug.Print(sql);
            DataTable dt = new DataTable();
            TfSQL tf = new TfSQL();
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql, ref dt);

            if (dt.Rows.Count != 0)
            {
                bool warnedAlready = false;
                string sql2 = " select sum(case when tm1.test_result in ('n/a', 'PASS') then 1 else 0 end) as okcount" +
                          "      , count(tm1.test_result) as totalcount" +
                          " from t_tray " +
                          " left join t_module tm1 on tm1.tray_id = t_tray.tray_id " +
                          " where t_tray.tray_id = '" + tray + "'";
                DataTable dt2 = new DataTable();
                TfSQL tf2 = new TfSQL();
                tf2.sqlDataAdapterFillDatatableFromTrayGuardDb(sql2, ref dt2);
            
                if ((long)dt2.Rows[0]["okcount"] != 24)
                {
                    MessageBox.Show("OK module is not 24", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    warnedAlready = true;
                    //if (txtLoginDept.Text != "PC") return;
                }
                if ((long)dt2.Rows[0]["totalcount"] != 24 && !warnedAlready)
                {
                    MessageBox.Show("Module data is not 24", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    warnedAlready = true;
                    //if (txtLoginDept.Text != "PC") return;
                }

                if ((int)dt.Rows[0]["qty"] != 24 && !warnedAlready)
                {
                    MessageBox.Show("Module in tray is not 24.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    warnedAlready = true;
                    //if (txtLoginDept.Text != "PC") return;
                }
            }

            // �悸�́A�V���A���̍\���v�f�̃p�^�[�����K�����A���[�U�[�f�X�N�g�b�v�̐ݒ�t�@�C�����g�p���Ċm�F����
            // 2017/03/07 Fujii �\���v�f�`�F�b�N�̃I�t
            //string matchResult = matchSerialNumberingPattern(tray);
            string matchResult = string.Empty;
            if (matchResult != string.Empty)
            {
                MessageBox.Show(matchResult + " does not match with desktop file's setting.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // �e�X�^�[�f�[�^�ɊY�����Ȃ��ꍇ�ł��A���[�U�[�F���p�ɕ\�����邽�߂̏���
            DataRow dr = dtTray.NewRow();
            dr["tray_id"] = tray;
            // �e�X�^�[�f�[�^�ɊY��������ꍇ�̏���
            if (dt.Rows.Count != 0)
            {
                dr["lot"] = (string)dt.Rows[0]["lot"];
                dr["qty"] = (int)dt.Rows[0]["qty"];
                dr["register_date"] = (DateTime)dt.Rows[0]["register_date"];
                dr["rg_dept"] = (string)dt.Rows[0]["rg_dept"];
                dr["multi_lot"] = (string)dt.Rows[0]["multi_lot"];
                dr["check"] = (string)dt.Rows[0]["check"];
            }

            // ��������̃e�[�u���Ƀ��R�[�h��ǉ�
            dtTray.Rows.Add(dr);

            // �f�[�^�O���b�g�r���[�̍X�V
            updateDataGridViews(dtTray, ref dgvTray, false);
        }

        // �T�u�v���V�[�W���F�V���A���̍\���v�f�̃p�^�[�����K�����A���[�U�[�f�X�N�g�b�v�̐ݒ�t�@�C�����g�p���Ċm�F����
        private string matchSerialNumberingPattern(string tray)
        {
            string result = string.Empty;

            string sql = "select module_id, substr(module_id, 1, 3) as plant, substr(module_id, 4, 1) as year, substr(module_id, 5, 2) as week, " +
                "substr(module_id, 7, 1) as day, substr(module_id, 8, 1) as line, substr(module_id, 12, 4) as eeee, " +
                "substr(module_id, 16, 1) as revision, substr(module_id, 19, 1) as mass, substr(module_id, 20, 1) as flexure, " +
                "substr(module_id, 21, 1) as cover_base, substr(module_id, 22, 2) as dframe, substr(module_id, 23, 1) as fpc, " +
                "substr(module_id, 24, 1) as shift from t_module where tray_id = '" + tray + "'";
            System.Diagnostics.Debug.Print(sql);
            DataTable dt = new DataTable();
            TfSQL tf = new TfSQL();
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql, ref dt);

            // �c�a����擾�������W���[���\���v�f�̃��X�g���A�f�X�N�g�b�v�Ŏw�肳�ꂽ���X�g�Ńt�B���^�[���|���A�Y����������x�����b�Z�[�W�ɒǉ�����
            if (!plant1.Any(s => s == "XXX"))
            {
                List<string> plant2 = dt.AsEnumerable().Select(r => r.Field<string>("plant")).Except(plant1).ToList();
                foreach(var e in plant2) result += "Plant '" + e + "'" + Environment.NewLine;
            }
            if (!year1.Any(s => s == "XXX"))
            {
                List<string> year2 = dt.AsEnumerable().Select(r => r.Field<string>("year")).Except(year1).ToList();
                foreach (var e in year2) result += "Year '" + e + "'" + Environment.NewLine;
            }
            if (!week1.Any(s => s == "XXX"))
            {
                List<string> week2 = dt.AsEnumerable().Select(r => r.Field<string>("week")).Except(week1).ToList();
                foreach (var e in week2) result += "Week '" + e + "'" + Environment.NewLine;
            }
            if (!day1.Any(s => s == "XXX"))
            {
                List<string> day2 = dt.AsEnumerable().Select(r => r.Field<string>("day")).Except(day1).ToList();
                foreach (var e in day2) result += "Day '" + e + "'" + Environment.NewLine;
            }
            if (!line1.Any(s => s == "XXX"))
            {
                List<string> line2 = dt.AsEnumerable().Select(r => r.Field<string>("line")).Except(line1).ToList();
                foreach (var e in line2) result += "Line '" + e + "'" + Environment.NewLine;
            }
            if (!eeee1.Any(s => s == "XXX"))
            {
                List<string> eeee2 = dt.AsEnumerable().Select(r => r.Field<string>("eeee")).Except(eeee1).ToList();
                foreach (var e in eeee2) result += "4E '" + e + "'" + Environment.NewLine;
            }
            if (!revision1.Any(s => s == "XXX"))
            {
                List<string> revision2 = dt.AsEnumerable().Select(r => r.Field<string>("revision")).Except(revision1).ToList();
                foreach (var e in revision2) result += "Revision '" + e + "'" + Environment.NewLine;
            }
            if (!mass1.Any(s => s == "XXX"))
            {
                List<string> mass2 = dt.AsEnumerable().Select(r => r.Field<string>("mass")).Except(mass1).ToList();
                foreach (var e in mass2) result += "Mass '" + e + "'" + Environment.NewLine;
            }
            if (!flexure1.Any(s => s == "XXX"))
            {
                List<string> flexure2 = dt.AsEnumerable().Select(r => r.Field<string>("flexure")).Except(flexure1).ToList();
                foreach (var e in flexure2) result += "Flexure '" + e + "'" + Environment.NewLine;
            }
            if (!cover_base1.Any(s => s == "XXX"))
            {
                List<string> cover_base2 = dt.AsEnumerable().Select(r => r.Field<string>("cover_base")).Except(cover_base1).ToList();
                foreach (var e in cover_base2) result += "Cover/base '" + e + "'" + Environment.NewLine;
            }
            if (!dframe1.Any(s => s == "XXX"))
            {
                List<string> dframe2 = dt.AsEnumerable().Select(r => r.Field<string>("dframe")).Except(dframe1).ToList();
                foreach (var e in dframe2) result += "Dframe '" + e + "'" + Environment.NewLine;
            }
            if (!fpc1.Any(s => s == "XXX"))
            {
                List<string> fpc2 = dt.AsEnumerable().Select(r => r.Field<string>("fpc")).Except(fpc1).ToList();
                foreach (var e in fpc2) result += "Fpc '" + e + "'" + Environment.NewLine;
            }
            if (!shift1.Any(s => s == "XXX"))
            {
                List<string> shift2 = dt.AsEnumerable().Select(r => r.Field<string>("shift")).Except(shift1).ToList();
                foreach (var e in shift2) result += "Shift '" + e + "'" + Environment.NewLine;
            }

            return result;
            //for (int i = 0; i < dt.Rows.Count; i++)
            //{
            //    if (!plant1.Any(s=> s=="XXX") && dt.Rows[i]["plant"].ToString() != plant) result += module + " Plant '" + dt.Rows[i]["plant"].ToString() + "'" + Environment.NewLine;
            //    else if (!year1.Any(s=> s=="XXX") && dt.Rows[i]["year"].ToString() != year) result += module + " Year '" + dt.Rows[i]["year"].ToString() + "'" + Environment.NewLine;
            //    else if (!week1.Any(s=> s=="XXX") && dt.Rows[i]["week"].ToString() != week) result += module + " Week '" + dt.Rows[i]["week"].ToString() + "'" + Environment.NewLine;
            //    else if (!day1.Any(s=> s=="XXX") && dt.Rows[i]["day"].ToString() != day) result += module + " Day '" + dt.Rows[i]["day"].ToString() + "'" + Environment.NewLine;
            //    else if (!line1.Any(s=> s=="XXX") && dt.Rows[i]["line"].ToString() != line) result += module + " Line '" + dt.Rows[i]["line"].ToString() + "'" + Environment.NewLine;
            //    else if (!eeee1.Any(s=> s=="XXX") && dt.Rows[i]["eeee"].ToString() != eeee) result += module + " 4E '" + dt.Rows[i]["eeee"].ToString() + "'" + Environment.NewLine;
            //    else if (!revision1.Any(s=> s=="XXX") && dt.Rows[i]["revision"].ToString() != revision) result += module + " Revision '" + dt.Rows[i]["revision"].ToString() + "'" + Environment.NewLine;
            //    else if (!mass1.Any(s=> s=="XXX") && dt.Rows[i]["mass"].ToString() != mass) result += module + " Mass '" + dt.Rows[i]["mass"].ToString() + "'" + Environment.NewLine;
            //    else if (!flexure1.Any(s=> s=="XXX") && dt.Rows[i]["flexure"].ToString() != flexure) result += module + " Flexure '" + dt.Rows[i]["flexure"].ToString() + "'" + Environment.NewLine;
            //    else if (!cover_base1.Any(s=> s=="XXX") && dt.Rows[i]["cover_base"].ToString() != cover_base) result += module + " Cover/base '" + dt.Rows[i]["cover_base"].ToString() + "'" + Environment.NewLine;
            //    else if (!dframe1.Any(s=> s=="XXX") && dt.Rows[i]["dframe"].ToString() != dframe) result += module + " D-Frame '" + dt.Rows[i]["dframe"].ToString() + "'" + Environment.NewLine;
            //    else if (!fpc1.Any(s=> s=="XXX") && dt.Rows[i]["fpc"].ToString() != fpc) result += module + " FPC '" + dt.Rows[i]["fpc"].ToString() + "'" + Environment.NewLine;
            //    else if (!shift1.Any(s=> s=="XXX") && dt.Rows[i]["shift"].ToString() != shift) result += module + " Shift '" + dt.Rows[i]["shift"].ToString() + "'" + Environment.NewLine;
            //}
        }

        // �o�^�{�^���������A�e��m�F�A�{�b�N�X�h�c�̔��s�A�V���A���̓o�^�A�o�[�R�[�h���x���̃v�����g���s��
        private void btnRegisterTray_Click(object sender, EventArgs e)
        {
            if (getOkCount(dtTray) != dtTray.Rows.Count)
            {
                MessageBox.Show("Tray is not 13.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            if (txtLoginDept.Text != "MFG")
            {
                //�{�����[�h�̏ꍇ�́A�v�����g�A�E�g
                if (!formAddMode)
                {
                    string pack = txtPackId.Text;
                    TfSato tfs = new TfSato();

                    // �r���`���A�r���a���A���[�U�[�ɑI��������
                    //DialogResult binResult = MessageBox.Show("Please click YES for Bin A, NO for Bin B.", "Print Option",
                    //    MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                    //string userBin = (binResult == DialogResult.Yes) ? "A" : "B";
                    string userBin = cmbBatch.Text;

                    // �y�K�g�����p���ۂ��A���[�U�[�ɑI��������
                    //DialogResult result = MessageBox.Show("Do you print Pegatoron label also?", "Print Option", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                    //if (result == DialogResult.Yes)
                    //{
                    tfs.printStart("packCartonInternal", pack, dtLot, cmbBatch.Text, dtpRegisterDate.Value, "Pack", "Pega", 1, userBin);
                    tfs.printStart("packCartonPega", pack, dtLot, cmbBatch.Text, dtpRegisterDate.Value, "Pack", "Pega", 1, userBin);
                    //}
                    //else
                    //{
                    //    tfs.printStart("packCartonInternal", pack, dtLot, cmbBatch.Text, dtpRegisterDate.Value, "Pack", "Fox", 1, userBin);
                    //}
                    return;
                }
            }

            //�ȉ��ǉ����[�h�̏ꍇ�̏���

            // 2016.08.22 FUJII  �p�b�N�h�c�̐V�K�̔ԃv���V�[�W���ugetNewPackId�v���A�g�����U�N�V���������o�[�W�����֕ύX
            //if (cmbBatch.Text == string.Empty)
            //{
            //    MessageBox.Show("Please select Batch.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
            //    return;
            //}

            // �o�^�������́A����{�^���������A���ׂẴR���g���[���𖳌��ɂ���
            cmbBatch.Enabled = false;
            txtTrayId.Enabled = false;
            btnRegisterPack.Enabled = false;
            btnDeleteSelection.Enabled = false;

            //�p�b�N�h�c�̐V�K�̔�
            string packNew = txtPackId.Text; // �p�b�N���e�ύX�̏��������{�^���ōs�����߁A�e�L�X�g�{�b�N�X�̊����h�c��ێ�����
            if (formAddMode)
            {
                TfSQL tf0 = new TfSQL();
                packNew = tf0.sqlGetNewPackId(maxLot, cmbBatch.Text, txtLoginName.Text, dtLot, ref registerDate);
                if (packNew == string.Empty)
                {
                    MessageBox.Show("An error happened in the pack id issuing process.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                txtPackId.Text = packNew;
            }

            if (txtLoginDept.Text == "MFG")
            {
                // �r���`���A�r���a���A���[�U�[�ɑI��������
                string pack = txtPackId.Text;
                TfSato tfs = new TfSato();
                //DialogResult binResult = MessageBox.Show("Please click YES for Bin A, NO for Bin B.", "Print Option",
                //    MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                //string userBin = (binResult == DialogResult.Yes) ? "A" : "B";
                string userBin = cmbBatch.Text;
                tfs.printStart("packCartonInternal", pack, dtLot, cmbBatch.Text, dtpRegisterDate.Value, "Pack", "Non", 1, userBin);
                if (!formAddMode)
                {
                    return;
                }
            }

            //�g���[�e�[�u���̃t�B�[���h�o�`�b�j�h�c�A���̑����X�V����
            TfSQL tf1 = new TfSQL();
            bool res = tf1.sqlMultipleUpdateTrayInPack(dtTray, packNew);

            if (res)
            {
                //�o�^�ς݂̏�Ԃ�\��
                txtPackId.Text = packNew;
                dtpRegisterDate.Value = registerDate;

                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
                this.Focus();
                MessageBox.Show("Pack ID: " + packNew + Environment.NewLine +
                    "and its trays were registered.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //���[�U�[�ɂ�郁�b�Z�[�W�{�b�N�X�m�F��̏���
                txtPackId.Text = String.Empty;
                txtTrayId.Text = String.Empty;
                dtTray.Clear();
                capacity = 13;
                updateDataGridViews(dtTray, ref dgvTray, false);
            }
        }

        // �X�[�p�[���[�U�[�Ɍ���A�o�^�σp�b�N���L�����Z���ł���i�J�[�g��������͕s�j
        private void btnCancelPack_Click(object sender, EventArgs e)
        {
            // �{���ɍ폜���Ă悢���A�Q�d�Ŋm�F����B
            DialogResult result1 = MessageBox.Show("Do you really want to cancel this tray?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result1 == DialogResult.No) return;

            DialogResult result2 = MessageBox.Show("Are you really sure? Please select NO if you are not sure.", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result2 == DialogResult.No) return;

            // �L�����Z���̎��s
            string packId = txtPackId.Text;
            TfSQL tf = new TfSQL();
            bool res = tf.sqlCancelTrayInPack(dtTray,packId, txtLoginName.Text);
            if (res)
            {
                //�{�t�H�[���̃f�[�^�O���b�g�r���[�X�V
                dtTray.Clear();
                updateDataGridViews(dtTray, ref dgvTray, false);

                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
                this.Focus();
                MessageBox.Show("Pack ID " + packId + " and its trays were canceled.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // ���b�Z�[�W�{�b�N�X�̊m�F��A����
                Close();
            }
            else
            {
                MessageBox.Show("Cancel process was not successful.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // �P�p�b�N������̃g���[����ύX����A�e�L�X�g�{�b�N�X�̃_�u���N���b�N����N��
        private void txtOkCount_DoubleClick(object sender, EventArgs e)
        {
            //if (userRole != "super") return;

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
                updateDataGridViews(dtTray, ref dgvTray, false);
            };

            fC.updateControls(capacity);
            fC.Show();
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

            Close();
        }

        // �T�u�v���V�[�W���F�L�����Z���ς݂܂��̓p�b�N�ς݂̃g���[���}�[�L���O����
        private void colorViewForCanceledAndPacked(ref DataGridView dgv)
        {
            for (int i = 0; i < dgv.Rows.Count; ++i)
            {
                if (dgv["check", i].Value.ToString() != "OK")
                {
                    dgv["check", i].Style.BackColor = Color.Red;
                    soundAlarm();
                }
                else
                {
                    dgv["check", i].Style.BackColor = Color.FromKnownColor(KnownColor.Window);
                }
            }
        }

        // �T�u�v���V�[�W���F�d�����R�[�h�A�܂��͂P�Z���Q�d���͂��}�[�L���O����
        private void colorViewForDuplicateTray(ref DataGridView dgv)
        {
            if (dgv.Rows.Count <= 0) return;

            DataTable dt = ((DataTable)dgv.DataSource).Copy();
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                string module = dgv["tray_id", i].Value.ToString();
                DataRow[] dr = dt.Select("tray_id = '" + module + "'");
                if (dr.Length >= 2)
                {
                    dgv["tray_id", i].Style.BackColor = Color.Red;
                    soundAlarm();
                }
                else
                {
                    dgv["tray_id", i].Style.BackColor = Color.FromKnownColor(KnownColor.Window);
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
        private void dgvTray_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            DataTable dt = new DataTable();
            dt = (DataTable)dgvTray.DataSource;
            ExcelClass xl = new ExcelClass();
            xl.ExportToExcel(dt);
        }

        // �k�n�s�W�v�O���b�h�r���[���̃{�^�����������A�Ώۂ̃��W���[�����G�N�Z���t�@�C���֏o�͂���
        private void btnExportModule_Click(object sender, EventArgs e)
        {
            // 2016.08.29 FUJII �ʃX���b�h�ŏ����i�����X�s�[�h�΍�j
            var task = Task.Factory.StartNew(() =>
            {
                DataTable dt = new DataTable();
                string sql = "select *, '" + txtPackId.Text + "' as pack_id from t_module where tray_id in (" +
                             "select tray_id from t_tray where pack_id = '" + txtPackId.Text + "')";
                TfSQL tf = new TfSQL();
                System.Diagnostics.Debug.Print(sql);
                tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql, ref dt);
                ExcelClass xl = new ExcelClass();
                // 2016.08.29 FUJII �G�N�Z���ւ̏o�͂���A�f�X�N�g�b�v�b�r�u�ւ̏o�͂֕ύX
                xl.ExportToCsv(dt, System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\pack.csv");
            });
        }

        // �ꎞ�e�[�u���̑I�����ꂽ�������R�[�h���A�ꊇ����������
        private void btnDeleteSelection_Click(object sender, EventArgs e)
        {
            if (dtTray.Rows.Count <= 0) return;

            // �Z���̑I��͈͂��Q��ȏ�̏ꍇ�́A���b�Z�[�W�̕\���݂̂Ńv���V�[�W���𔲂���
            if (dgvTray.Columns.GetColumnCount(DataGridViewElementStates.Selected) >= 2)
            {
                MessageBox.Show("Please select range with only one column.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }

            DialogResult result = MessageBox.Show("Do you really want to delete the selected rows?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (result == DialogResult.No) return;

            foreach (DataGridViewCell cell in dgvTray.SelectedCells)
            {
                dtTray.Rows[cell.RowIndex].Delete();
            }
            dtTray.AcceptChanges();
            updateDataGridViews(dtTray, ref dgvTray, false);
            txtTrayId.Focus();
            txtTrayId.SelectAll();
        }

        //�g���[�O���b�h�r���[��̃{�^���̃N���b�N�ŁAformModuleInTray���Ăяo���A�Ή����郂�W���[����\������i�f���Q�[�g�Ȃ��j
        private void dgvTray_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int currentRow = int.Parse(e.RowIndex.ToString());

            if (dgvTray.Columns[e.ColumnIndex] == openTray && currentRow >= 0)
            {
                //����frmModuleInTray ���J����Ă���ꍇ�́A��������悤����
                if (TfGeneral.checkOpenFormExists("frmModuleInTray"))
                {
                    MessageBox.Show("Please close the currently open form.", "Notice",
                        MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                    return;
                }

                string trayId = dgvTray["tray_id", currentRow].Value.ToString();
                string line = VBS.Right(trayId, 1);
                DateTime trayDate = (DateTime)dgvTray["register_date", currentRow].Value;

                //�f���Q�[�g�Ȃ��B���g���[�t�H�[���̃t�H�[���ʒu���Q�̏ꍇ�́A���W���[���t�H�[���̈ʒu�͂R�B����ȊO�͂Q�B
                frmModuleInTray fM = new frmModuleInTray();
                fM.updateControls(trayId, trayDate, "", txtLoginName.Text, txtLoginDept.Text, userRole, line, false, false, true, true, 4, false, false);
                fM.Show();
            }
        }

        // ���ڃ}�X�^�o�^�t�H�[���̋N��
        private void cmbBatch_KeyDown(object sender, KeyEventArgs e)
        {
            // �X�[�p�[���[�U�[���g�p���̏ꍇ�̂݁A���[�U�[�}�X�^�[�ύX�t�H�[�����J��
            if (e.KeyCode != Keys.Enter || userRole != "super") return;
            if (TfGeneral.checkOpenFormExists("frmMasterCriteria")) return;
            frmMasterCriteria fI = new frmMasterCriteria("BATCH");
            //�q�C�x���g���L���b�`���āA�f�[�^�O���b�h���X�V����
            fI.RefreshEvent += delegate (object sndr, EventArgs excp)
            {
                //�o�b�`�R���{�{�b�N�X�̐ݒ�
                setBatchComboBox();
            };
            fI.Show();
        }


        //����{�^����V���[�g�J�b�g�ł̏I���������Ȃ�
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