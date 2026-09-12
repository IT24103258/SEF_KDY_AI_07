// Where each role lands right after login.
export const ROLE_HOME_PATHS = {
  Administrator: '/',
  Manager: '/',
  Technician: '/technician',
  Requester: '/my-requests'
};

export const getHomePath = (role) => ROLE_HOME_PATHS[role] || '/my-requests';