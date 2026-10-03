import 'package:flutter/material.dart';
import '../core/constants/api_constants.dart';
import '../core/errors/app_exception.dart';
import '../core/network/api_client.dart';
import '../core/storage/secure_storage_service.dart';
import '../models/user_model.dart';

class AuthProvider with ChangeNotifier {
  final ApiClient _apiClient;
  final SecureStorageService _storage;

  UserModel? _user;
  bool _isLoading = false;
  bool _isRestoring = false;
  String? _error;

  AuthProvider({ApiClient? apiClient, SecureStorageService? storage})
      : _apiClient = apiClient ?? ApiClient(),
        _storage = storage ?? SecureStorageService();

  UserModel? get user => _user;
  bool get isLoading => _isLoading;
  bool get isRestoring => _isRestoring;
  bool get isAuthenticated => _user != null;
  String? get error => _error;

  /// Restores a persisted JWT on app start by fetching GET /api/auth/me —
  /// mirrors React's initial auth check in AuthContext. A rejected/expired
  /// token clears storage and falls back to the login screen.
  Future<void> tryAutoLogin() async {
    try {
      final token = await _storage.getToken();
      if (token != null) {
        final res = await _apiClient.get(ApiConstants.currentUser);
        if (res['success'] == true && res['data'] != null) {
          _user = UserModel.fromJson(res['data']);
        } else {
          await _clearStoredToken();
        }
      }
    } catch (_) {
      await _clearStoredToken();
      _user = null;
    }
    _isRestoring = false;
    notifyListeners();
  }

  Future<void> _clearStoredToken() async {
    try {
      await _storage.deleteToken();
    } catch (_) {
      // Storage unavailable (e.g. plugin missing) — nothing more to clean up.
    }
  }

  /// AppException already carries the server's message verbatim. Anything else
  /// is a transport failure, whose toString() is a raw ClientException /
  /// SocketException description that is useless to an end user.
  static String _describeError(Object error) {
    if (error is AppException) return error.message;
    return 'Cannot reach the FixFlow API. Check your connection and try again.';
  }

  Future<bool> login(String email, String password) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final res = await _apiClient.post(ApiConstants.login, {
        'email': email,
        'password': password,
      });

      if (res['success'] == true && res['data'] != null) {
        final token = res['data']['token'] as String;
        await _storage.saveToken(token);
        _user = UserModel.fromJson(res['data']['user']);
        _isLoading = false;
        notifyListeners();
        return true;
      }
      _error = res['message']?.toString() ?? 'Sign in failed. Please try again.';
    } catch (e) {
      _error = _describeError(e);
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  /// POST /api/auth/register, then auto sign-in — mirrors the React flow
  /// (the register endpoint returns the created user but no token).
  Future<bool> register({
    required String firstName,
    required String lastName,
    required String email,
    required String phoneNumber,
    required String password,
    String roleName = 'Requester',
  }) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      await _apiClient.post(ApiConstants.register, {
        'firstName': firstName,
        'lastName': lastName,
        'email': email,
        'phoneNumber': phoneNumber,
        'password': password,
        'roleName': roleName,
      });
      _isLoading = false;
      notifyListeners();
      // Auto-login with the credentials just registered (same as React)
      return await login(email, password);
    } catch (e) {
      _error = _describeError(e);
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  Future<void> logout() async {
    await _storage.deleteToken();
    _user = null;
    notifyListeners();
  }
}
