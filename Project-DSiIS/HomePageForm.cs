﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Oracle.ManagedDataAccess.Client;
using Project_DSiIS.Project_DSiIS;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Project_DSiIS
{
    public partial class HomePageForm : Form
    {
        private OracleConnection _conn;
        private OracleSQLHandle _orl;

        public HomePageForm(OracleConnection conn)
        {
            InitializeComponent();
            _conn = conn;
            _orl = new OracleSQLHandle(conn);
            InitializeCreateUserDataGridView();
            tabControlHomePage_SelectedIndexChanged(tabControlHomePage, EventArgs.Empty);
            dataGridViewListUser.CellClick -= dataGridViewListUser_CellClick;
            dataGridViewListUser.SelectionChanged += dataGridViewListUser_SelectionChanged;

        }


        /*
         * Cac phan lien quan den giao dien
        */
        // Xử lý TabPage
        private void tabControlHomePage_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Xử lý TabPage Xem thông tin User
            if (tabControlHomePage.SelectedIndex == 0)
            {
                buttonShowUser_Click(sender, e);
                string queryStringGetDBName = "SELECT SYS_CONTEXT('USERENV', 'CON_NAME') FROM DUAL";
                string queryStringGetUserName = "SELECT SYS_CONTEXT('USERENV', 'CURRENT_USER') FROM DUAL";

                using (OracleCommand getUserNameCmd = new OracleCommand(queryStringGetUserName, _conn))
                using (OracleCommand getDBNameCmd = new OracleCommand(queryStringGetDBName, _conn))
                {
                    try
                    {
                        using (OracleDataReader userNameReader = getUserNameCmd.ExecuteReader())
                        using (OracleDataReader dbNameReader = getDBNameCmd.ExecuteReader())
                        {
                            userNameReader.Read();
                            dbNameReader.Read();
                            labelGreetingUser.Text = $"Xin chào user: {userNameReader.GetString(0)}, tên DB đang kết nối: {dbNameReader.GetString(0)}";
                        }
                    }
                    catch (OracleException ex)
                    {
                        MessageBox.Show($"Error occurred while retrieving user and database information: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            // Xử lý TabPage Xem thông tin về quyền
            else if (tabControlHomePage.SelectedIndex == 1)
            {
                //TODO: 
            }
            // Xử lý TabPage về User
            else if (tabControlHomePage.SelectedIndex == 2)
            {
                buttonListUserCreateUser_Click(sender, e);
                buttonListUser_Click(sender, e);
            }
            // Xử lý TabPage về Role
            else if (tabControlHomePage.SelectedIndex == 3)
            {
                buttonListRole_Click(sender, e);
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
            {
                buttonListUserCreateUser_Click(sender, e);
            }
            else if (tabControl1.SelectedIndex == 1)
            {
                buttonListUser_Click(sender, e);
            }
            else if (tabControl1.SelectedIndex == 3)
            {
                //ComboBox Danh sach user
                DataTable datatable = _orl.GetUserData("select * from all_users ORDER BY CREATED DESC");
                comboBoxUsers.ValueMember = "User";
                comboBoxUsers.DisplayMember = "UserName";
                comboBoxUsers.DataSource = datatable;
                comboBoxUsers.SelectedIndex = -1;

                //ComboBox Quyen He Thong
                DataTable datatable2 = _orl.GetUserData("select distinct privilege from dba_sys_privs");
                comboBoxSystemPrivileges.ValueMember = "Privilege";
                comboBoxSystemPrivileges.DisplayMember = "Privilege";
                comboBoxSystemPrivileges.DataSource = datatable2;
                comboBoxSystemPrivileges.SelectedIndex = -1;

                //ComboBox Table 
                DataTable dataTable3 = _orl.GetUserData("SELECT * FROM all_tables");
                comboBoxTable.ValueMember = "Table Name";
                comboBoxTable.DisplayMember = "Table_name";
                comboBoxTable.DataSource = dataTable3;
                comboBoxTable.SelectedIndex = -1;
            }
        }

        //Xử lý Event khi click vào button Xem danh sách user
        private void buttonShowUser_Click(object sender, EventArgs e)
        {
            string sp_name = "sp_get_all_users";
            _orl.GetUserandRole(OracleSQLHandle.SP.GetAllUsers, dataGridViewShowUser);
        }

        private void textBoxSearchUserName_MouseClick(object sender, MouseEventArgs e)
        {
            textBoxSearchUserName.Clear();
        }

        private void textBoxSearchUserName_TextChanged(object sender, EventArgs e)
        {
            string sp_name = "sp_get_user_by_username";
            _orl.GetUserandRole(OracleSQLHandle.SP.GetUserByUsername, dataGridViewShowUser, textBoxSearchUserName.Text);
        }

        private void InitializeCreateUserDataGridView()
        {
            DataTable datatable = new DataTable();
            datatable.Columns.Add("USERNAME");
            dataGridViewCreateUser.DataSource = datatable;
        }

        //Xử lý event khi click vào buttion Tạo User
        private void buttionCreateUser_Click(object sender, EventArgs e)
        {
            string sp_name = "sp_create_user";
            string username = textBoxCreateUserUsername.Text;

            if (textBoxCreateUserPassword.Text == textBoxCreateUsernameConfirmPassword.Text)
            {
                string password = textBoxCreateUserPassword.Text;
                var parameterDict = new Dictionary<string, object>
                {
                    {"p_username", username },
                    {"p_password", password },
                    {"p_create_session", checkBoxGrantCreateSession.Checked ? 1 : 0}
                };
                try
                {
                    _orl.ExecuteProcedureWithNoQuery(OracleSQLHandle.SP.CreateUser, parameterDict);
                    if (checkBoxGrantCreateSession.Checked)
                    {
                        MessageBox.Show(text: $"User {username} đã được tạo thành công và cấp quyền CREATE SESSION privilege.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"User {username} đã được tạo thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }

                    //Refesh dataGridViewCreateUser
                    buttonListUserCreateUser_Click(sender, e);
                    //Add user vừa tạo vào dataGridViewCreateUser
                    DataTable datatable = dataGridViewCreateUser.DataSource as DataTable;
                    DataRow newRow = datatable.NewRow();
                    newRow["USERNAME"] = username;
                    datatable.Rows.Add(newRow);
                    datatable.AcceptChanges();

                }
                catch (OracleException ex)
                {
                    MessageBox.Show($"Có lỗi khi thực hiện việc tạo User: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
            }
            else
            {
                MessageBox.Show("Mật khẩu và mật khẩu xác nhận không chính xác, vui lòng thử lại", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBoxCreateUserUsername.Clear();
                textBoxCreateUserPassword.Clear();
                textBoxCreateUsernameConfirmPassword.Clear();
            }
        }

        private void buttonClearDataCreateUser_Click(object sender, EventArgs e)
        {
            InitializeCreateUserDataGridView();
        }

        private void buttonDropUser_Click(object sender, EventArgs e)
        {
            string sp_name = "sp_drop_user";
            string username = textBoxDropUser.Text;
            DialogResult dr = MessageBox.Show($"Bạn có chắc là muốn xoá user: {username} ? ", "Xác nhận", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (dr == DialogResult.Yes)
            {
                var parameterDict = new Dictionary<string, object>
                {
                    { "p_username", username },
                    { "p_cascade", checkBoxDropUser.Checked ? 1 : 0}
                };
                try
                {
                    _orl.ExecuteProcedureWithNoQuery(OracleSQLHandle.SP.DropUser, parameterDict);
                    MessageBox.Show($"User {username} đã được xoá thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    buttonListUser_Click(sender, e);
                }
                catch (OracleException ex)
                {
                    MessageBox.Show($"Có lỗi khi thực hiện việc xoá User: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void buttonListUser_Click(object sender, EventArgs e)
        {
            string sp_name = "sp_get_all_users";
            _orl.GetUserandRole(OracleSQLHandle.SP.GetAllUsers, dataGridViewListUser);
        }



        private void dataGridViewListUser_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            string userName = dataGridViewListUser.CurrentRow.Cells[0].Value.ToString();
            textBoxDropUser.Text = userName;

        }

        private void dataGridViewListUser_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridViewListUser.CurrentRow != null)
            {
                string userName = dataGridViewListUser.CurrentRow.Cells[0].Value.ToString();
                textBoxDropUser.Text = userName;
            }
        }

        private void textBoxDropUser2_TextChanged(object sender, EventArgs e)
        {
            string sp_name = "sp_get_user_by_username";
            _orl.GetUserandRole(OracleSQLHandle.SP.GetUserByUsername, dataGridViewListUser, textBoxDropUser2.Text);
        }

        private void buttonListRole_Click(object sender, EventArgs e)
        {
            string sp_name = "sp_get_all_role";
            _orl.GetUserandRole(OracleSQLHandle.SP.GetAllRoles, dataGridViewListRole);
        }

        private void textBoxSreachListRole_TextChanged(object sender, EventArgs e)
        {

            string sp_name = "sp_get_role_by_rolename";
            _orl.GetUserandRole(OracleSQLHandle.SP.GetRoleByRoleName, dataGridViewListRole, textBoxSreachListRole.Text);
        }

        private void buttonCreateRole_Click(object sender, EventArgs e)
        {
            string sp_name = "sp_create_role";
            string roleName = textBoxRolename.Text;
            string password = textBoxRolenamePassword.Text;
            Dictionary<string, object> parameterDict;

            if (String.IsNullOrWhiteSpace(password) == false)
            {
                parameterDict = new Dictionary<string, object>
                {
                    { "p_role_name", roleName },
                    { "p_password_optional", password },
                    { "p_password_optional_checked", 1 }
                };
            }
            else
            {
                parameterDict = new Dictionary<string, object>
                {
                    { "p_role_name", roleName },
                    { "p_password_optional", null },
                    { "p_password_optional_checked", 0 }
                };
            }

            try
            {
                _orl.ExecuteProcedureWithNoQuery(OracleSQLHandle.SP.CreateRole, parameterDict);
                MessageBox.Show($"Role {roleName} đã được tạo thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonListRole_Click(sender, e);
            }
            catch (OracleException ex)
            {
                MessageBox.Show($"Có lỗi khi thực hiện việc tạo Role: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBoxListUserCreateUser_TextChanged(object sender, EventArgs e)
        {
            string sp_name = "sp_get_user_by_username";
            _orl.GetUserandRole(OracleSQLHandle.SP.GetUserByUsername, dataGridViewListUserCreateUser, textBoxListUserCreateUser.Text);
        }

        private void buttonListUserCreateUser_Click(object sender, EventArgs e)
        {
            string sp_name = "sp_get_all_users";
            _orl.GetUserandRole(OracleSQLHandle.SP.GetAllUsers, dataGridViewListUserCreateUser);
        }

        private void buttonDropRole_Click(object sender, EventArgs e)
        {
            string sp_name = "sp_drop_role";
            string rolename = textBoxDropRole.Text;
            DialogResult dr = MessageBox.Show($"Bạn có chắc là muốn xoá role: {rolename} ? ", "Xác nhận", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (dr == DialogResult.Yes)
            {
                var parameterDict = new Dictionary<string, object>
                {
                    { "p_role_name", rolename }
                };
                try
                {
                    _orl.ExecuteProcedureWithNoQuery(sp_name, parameterDict);
                    MessageBox.Show($"User {rolename} đã được xoá thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    buttonViewRoleInDropRoleTab_Click(sender, e);
                }
                catch (OracleException ex)
                {
                    MessageBox.Show($"Có lỗi khi thực hiện việc xoá role: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void buttonViewRoleInDropRoleTab_Click(object sender, EventArgs e)
        {
            string sp_name = "sp_get_all_role";
            _orl.GetUserandRole(OracleSQLHandle.SP.GetAllRoles, dataGridViewDropRole);
        }

        private void textBoxViewRoleInDropRoleTab_TextChanged(object sender, EventArgs e)
        {
            string sp_name = "sp_get_role_by_rolename";
            _orl.GetUserandRole(OracleSQLHandle.SP.GetRoleByRoleName, dataGridViewDropRole, textBoxViewRoleInDropRoleTab.Text);
        }

        private void buttonEditUserPassword_Click(object sender, EventArgs e)
        {
            string username = textBoxEditUserUsername.Text;

            if (textboxEditUserPassword.Text == textBoxEditUserConfirmPassword.Text)
            {
                DialogResult dr = MessageBox.Show($"Bạn có chắc là muốn đổi mật khẩu của: {username} ? ", "Xác nhận", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (dr == DialogResult.Yes)
                {
                    string password = textboxEditUserPassword.Text;
                    var parameterDict = new Dictionary<string, object>
                    {
                        {"p_username", username },
                        {"p_new_password", password }
                    };
                    try
                    {
                        _orl.ExecuteProcedureWithNoQuery(OracleSQLHandle.SP.EditUserPassword, parameterDict);
                        MessageBox.Show($"User {username} đã được cập nhật mật khẩu thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (OracleException ex)
                    {
                        MessageBox.Show($"Có lỗi khi thực hiện việc tạo chỉnh sửa mật khẩu của User: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    }
                }

            }
            else
            {
                MessageBox.Show("Mật khẩu và mật khẩu xác nhận không chính xác, vui lòng thử lại", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textboxEditUserPassword.Clear();
                textBoxEditUserConfirmPassword.Clear();
                textBoxEditUserUsername.Clear();
            }
        }

        private void buttonPrivilUser_Click(object sender, EventArgs e)
        {
            _orl.GetUserandRole(OracleSQLHandle.SP.GetUserPrivileges, dataGridViewPrivilUser);
        }

        private void buttonPrivilRole_Click(object sender, EventArgs e)
        {
            _orl.GetUserandRole(OracleSQLHandle.SP.GetRolePrivileges, dataGridViewRoles);
        }

        private void buttonGrantUser_Click(object sender, EventArgs e)
        {
            string user = comboBoxUsers.Text;
            string objPrivil = comboBoxObjectPrivileges.Text;
            string sysPrivil = comboBoxSystemPrivileges.Text;
            string objName = comboBoxTable.Text;
            Dictionary<string, object> parameterDict = null;
            if (!String.IsNullOrWhiteSpace(user) && !String.IsNullOrWhiteSpace(objPrivil) && String.IsNullOrEmpty(sysPrivil))
            {
                MessageBox.Show($"{user},{objPrivil},{sysPrivil}");
                parameterDict = new Dictionary<string, object>
                {
                    {"p_user_name", user },
                    {"p_permission", objPrivil },
                    {"p_object_name", objName},
                    {"p_with_grant_option", checkBoxWithGrantOption.Checked ? 1 : 0},
                    {"p_is_system_privilege", 0 }
                };
            }
            else if (!String.IsNullOrWhiteSpace(user) && String.IsNullOrWhiteSpace(objPrivil) && !String.IsNullOrEmpty(sysPrivil))
            {
                parameterDict = new Dictionary<string, object>
                {
                    {"p_user_name", user },
                    {"p_permission", sysPrivil },
                    {"p_object_name", objName},
                    {"p_with_grant_option", checkBoxWithGrantOption.Checked ? 1 : 0},
                    {"p_is_system_privilege", 1 }

                };
            }
            try
            {
                _orl.ExecuteProcedureWithNoQuery(OracleSQLHandle.SP.GrantPremissionToUser, parameterDict);
                MessageBox.Show($"Gán quyền thành công ", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OracleException ex)
            {
                MessageBox.Show($"Có lỗi khi thực hiện việc gán quyền cho User: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void comboBoxObjectPrivileges_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(comboBoxObjectPrivileges.Text))
            {
                comboBoxSystemPrivileges.Enabled = false;
            }
            else
            {
                comboBoxSystemPrivileges.Enabled = true;
            }

        }
    }
}
