import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:provider/provider.dart';

import 'package:fixflow_mobile/core/errors/app_exception.dart';
import 'package:fixflow_mobile/core/network/api_client.dart';
import 'package:fixflow_mobile/core/routes/app_router.dart';
import 'package:fixflow_mobile/core/storage/secure_storage_service.dart';
import 'package:fixflow_mobile/core/theme/app_theme.dart';
import 'package:fixflow_mobile/providers/auth_provider.dart';
import 'package:fixflow_mobile/providers/theme_provider.dart';
import 'package:fixflow_mobile/screens/home_screen.dart';
import 'package:fixflow_mobile/screens/login_screen.dart';
import 'package:fixflow_mobile/screens/priority_details_screen.dart';
import 'package:fixflow_mobile/screens/register_screen.dart';

// =============================================================================
// Focused coverage for the Login/Register screens and for the Component 2 home
// screen cleanup. The production auth path is unchanged: Flutter talks to the
// ASP.NET Core API only. The http.Client is replaced with a test double so the
// real request (endpoint, method, JSON body) can be asserted; nothing here
// grants a sign-in that the backend did not return.
// =============================================================================

/// In-memory stand-in for the plugin-backed store, so the JWT persistence step
/// can be observed without the flutter_secure_storage platform channel.
class _MemoryStorage extends SecureStorageService {
  String? token;
  int saves = 0;

  @override
  Future<void> saveToken(String value) async {
    token = value;
    saves++;
  }

  @override
  Future<String?> getToken() async => token;

  @override
  Future<void> deleteToken() async {
    token = null;
  }
}

/// Records every request the app issues.
class _Recorder {
  final List<http.Request> requests = [];

  List<String> get paths => requests
      .map((r) => r.url.path + (r.url.query.isEmpty ? '' : '?${r.url.query}'))
      .toList();

  Map<String, dynamic> body(int index) =>
      jsonDecode(requests[index].body) as Map<String, dynamic>;
}

Map<String, dynamic> _envelope(Object? data, {String message = 'OK'}) =>
    <String, dynamic>{'success': true, 'message': message, 'data': data};

Map<String, dynamic> _loginResponse({
  String role = 'Administrator',
  String email = 'admin@fixflow.local',
}) =>
    _envelope(<String, dynamic>{
      'token': 'jwt-for-$role',
      'expiresAt': '2026-10-03T12:00:00Z',
      'user': <String, dynamic>{
        'id': '3f1c9a52-0d5a-4b93-9d21-6f1a0a1b2c3d',
        'email': email,
        'firstName': 'Test',
        'lastName': role,
        'phoneNumber': '+94770000001',
        'role': role,
      },
    }, message: 'Login successful');

http.Client _stub({
  required _Recorder recorder,
  required int status,
  required Object body,
}) {
  return MockClient((http.Request request) async {
    recorder.requests.add(request);
    return http.Response(
      jsonEncode(body),
      status,
      headers: <String, String>{'content-type': 'application/json'},
    );
  });
}

/// A client whose response never arrives, used to freeze a screen in its
/// loading state.
http.Client _hanging({required _Recorder recorder}) {
  return MockClient((http.Request request) {
    recorder.requests.add(request);
    return Completer<http.Response>().future;
  });
}

/// POST /api/auth/register succeeds, then the auto sign-in succeeds.
http.Client _registerThenLogin({required _Recorder recorder, String role = 'Requester'}) {
  return MockClient((http.Request request) async {
    recorder.requests.add(request);
    final Object body = request.url.path == '/api/auth/register'
        ? _envelope(<String, dynamic>{
            'id': '9d2f1a00-1111-2222-3333-444455556666',
            'email': 'ada@example.com',
            'firstName': 'Ada',
            'lastName': 'Lovelace',
            'phoneNumber': '+94771234567',
            'role': role,
          }, message: 'User registered successfully')
        : _loginResponse(role: role, email: 'ada@example.com');
    return http.Response(
      jsonEncode(body),
      200,
      headers: <String, String>{'content-type': 'application/json'},
    );
  });
}

AuthProvider _auth({required http.Client client, required _MemoryStorage storage}) =>
    AuthProvider(
      apiClient: ApiClient(client: client, storage: storage),
      storage: storage,
    );

/// Signs in against a stubbed API so a role-specific HomeScreen can be pumped.
Future<AuthProvider> _signedIn(String role) async {
  final _MemoryStorage storage = _MemoryStorage();
  final AuthProvider auth = _auth(
    client: _stub(
      recorder: _Recorder(),
      status: 200,
      body: _loginResponse(role: role, email: '${role.toLowerCase()}@fixflow.local'),
    ),
    storage: storage,
  );
  await auth.login('${role.toLowerCase()}@fixflow.local', 'Passw0rd1');
  return auth;
}

Widget _appWith(AuthProvider auth, Widget child) {
  return MultiProvider(
    providers: [
      ChangeNotifierProvider<AuthProvider>.value(value: auth),
      ChangeNotifierProvider<ThemeProvider>(create: (_) => ThemeProvider()),
    ],
    child: MaterialApp(theme: AppTheme.lightTheme, home: child),
  );
}

/// Uses the real AppRouter so navigation assertions exercise production routes.
Widget _routedApp(AuthProvider auth, {String initial = AppRouter.login}) {
  return MultiProvider(
    providers: [
      ChangeNotifierProvider<AuthProvider>.value(value: auth),
      ChangeNotifierProvider<ThemeProvider>(create: (_) => ThemeProvider()),
    ],
    child: MaterialApp(
      theme: AppTheme.lightTheme,
      initialRoute: initial,
      onGenerateRoute: AppRouter.generateRoute,
    ),
  );
}

/// The auth forms are taller than the default 800x600 test surface; enlarge it
/// so every field and button is actually hittable.
void _useLargeSurface(WidgetTester tester) {
  tester.view.physicalSize = const Size(1100, 2600);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);
}

/// Reads the masking flag off the TextField that a keyed TextFormField builds.
bool _obscured(WidgetTester tester, Key key) {
  return tester
      .widget<TextField>(find.descendant(
        of: find.byKey(key),
        matching: find.byType(TextField),
      ))
      .obscureText;
}

void main() {
  // ===========================================================================
  // Login screen
  // ===========================================================================
  group('LoginScreen', () {
    testWidgets('L1. renders the branded login form', (tester) async {
      _useLargeSurface(tester);
      final AuthProvider auth = _auth(
        client: _stub(recorder: _Recorder(), status: 200, body: _envelope(null)),
        storage: _MemoryStorage(),
      );

      await tester.pumpWidget(_appWith(auth, const LoginScreen()));
      await tester.pumpAndSettle();

      expect(find.text('FixFlow AI'), findsOneWidget);
      expect(find.text('Field Technician & Requester Portal'), findsOneWidget);
      expect(find.byKey(const Key('login_email_field')), findsOneWidget);
      expect(find.byKey(const Key('login_password_field')), findsOneWidget);
      expect(find.byKey(const Key('login_submit_button')), findsOneWidget);
      expect(find.text('Sign In'), findsOneWidget);
      expect(find.text("Don't have an account? Register"), findsOneWidget);
    });

    testWidgets('L2. never pre-fills or displays stored credentials',
        (tester) async {
      _useLargeSurface(tester);
      final AuthProvider auth = _auth(
        client: _stub(recorder: _Recorder(), status: 200, body: _envelope(null)),
        storage: _MemoryStorage(),
      );

      await tester.pumpWidget(_appWith(auth, const LoginScreen()));
      await tester.pumpAndSettle();

      // The previously hard-coded seeded technician account must be gone.
      expect(find.text('tech@fixflow.local'), findsNothing);
      expect(find.text('Tech123!'), findsNothing);
      expect(find.textContaining('Tech123'), findsNothing);

      final email = tester.widget<TextFormField>(
        find.byKey(const Key('login_email_field')),
      );
      final password = tester.widget<TextFormField>(
        find.byKey(const Key('login_password_field')),
      );
      expect(email.controller?.text ?? '', isEmpty);
      expect(password.controller?.text ?? '', isEmpty);
      expect(_obscured(tester, const Key('login_password_field')), isTrue,
          reason: 'the password must be masked until explicitly revealed');
    });

    testWidgets('L3. the visibility toggle reveals and re-masks the password',
        (tester) async {
      _useLargeSurface(tester);
      final AuthProvider auth = _auth(
        client: _stub(recorder: _Recorder(), status: 200, body: _envelope(null)),
        storage: _MemoryStorage(),
      );
      await tester.pumpWidget(_appWith(auth, const LoginScreen()));
      await tester.pumpAndSettle();

      bool obscured() =>
          _obscured(tester, const Key('login_password_field'));

      await tester.tap(find.byKey(const Key('login_password_toggle')));
      await tester.pumpAndSettle();
      expect(obscured(), isFalse);

      await tester.tap(find.byKey(const Key('login_password_toggle')));
      await tester.pumpAndSettle();
      expect(obscured(), isTrue);
    });

    testWidgets('L4. an empty submit is rejected locally and never calls the API',
        (tester) async {
      _useLargeSurface(tester);
      final _Recorder recorder = _Recorder();
      final AuthProvider auth = _auth(
        client: _stub(recorder: recorder, status: 200, body: _envelope(null)),
        storage: _MemoryStorage(),
      );
      await tester.pumpWidget(_appWith(auth, const LoginScreen()));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('login_submit_button')));
      await tester.pumpAndSettle();

      expect(find.text('Email is required'), findsOneWidget);
      expect(find.text('Password is required'), findsOneWidget);
      expect(recorder.requests, isEmpty);
      expect(auth.isAuthenticated, isFalse);
    });

    testWidgets('L5. a malformed email is rejected locally', (tester) async {
      _useLargeSurface(tester);
      final _Recorder recorder = _Recorder();
      final AuthProvider auth = _auth(
        client: _stub(recorder: recorder, status: 200, body: _envelope(null)),
        storage: _MemoryStorage(),
      );
      await tester.pumpWidget(_appWith(auth, const LoginScreen()));
      await tester.pumpAndSettle();

      await tester.enterText(find.byKey(const Key('login_email_field')), 'not-an-email');
      await tester.enterText(find.byKey(const Key('login_password_field')), 'Admin123!');
      await tester.tap(find.byKey(const Key('login_submit_button')));
      await tester.pumpAndSettle();

      expect(find.text('Enter a valid email address'), findsOneWidget);
      expect(recorder.requests, isEmpty);
    });

    testWidgets('L6. the register link opens the Registration screen',
        (tester) async {
      _useLargeSurface(tester);
      final AuthProvider auth = _auth(
        client: _stub(recorder: _Recorder(), status: 200, body: _envelope(null)),
        storage: _MemoryStorage(),
      );
      await tester.pumpWidget(_routedApp(auth));
      await tester.pumpAndSettle();

      expect(find.byType(LoginScreen), findsOneWidget);

      await tester.tap(find.byKey(const Key('login_register_link')));
      await tester.pumpAndSettle();

      expect(find.byType(RegisterScreen), findsOneWidget);
      expect(find.text('Join FixFlow'), findsOneWidget);
    });

    testWidgets('L7. the loading state disables submit and blocks a second request',
        (tester) async {
      _useLargeSurface(tester);
      final _Recorder recorder = _Recorder();
      final _MemoryStorage storage = _MemoryStorage();
      final AuthProvider auth = _auth(
        client: _hanging(recorder: recorder),
        storage: storage,
      );
      await tester.pumpWidget(_appWith(auth, const LoginScreen()));
      await tester.pumpAndSettle();

      await tester.enterText(find.byKey(const Key('login_email_field')), 'admin@fixflow.local');
      await tester.enterText(find.byKey(const Key('login_password_field')), 'Admin123!');
      await tester.tap(find.byKey(const Key('login_submit_button')));
      await tester.pump();

      expect(auth.isLoading, isTrue);
      expect(
        tester.widget<ElevatedButton>(find.byKey(const Key('login_submit_button'))).onPressed,
        isNull,
      );
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(storage.saves, 0);

      await tester.tap(find.byKey(const Key('login_submit_button')), warnIfMissed: false);
      await tester.pump();

      expect(recorder.requests, hasLength(1),
          reason: 'a second tap must not issue a second login request');
    });

    testWidgets('L8. a successful login reaches the role home screen',
        (tester) async {
      _useLargeSurface(tester);
      final _Recorder recorder = _Recorder();
      final _MemoryStorage storage = _MemoryStorage();
      final AuthProvider auth = _auth(
        client: _stub(recorder: recorder, status: 200, body: _loginResponse()),
        storage: storage,
      );
      await tester.pumpWidget(_routedApp(auth));
      await tester.pumpAndSettle();

      await tester.enterText(find.byKey(const Key('login_email_field')), 'admin@fixflow.local');
      await tester.enterText(find.byKey(const Key('login_password_field')), 'Admin123!');
      await tester.tap(find.byKey(const Key('login_submit_button')));
      await tester.pumpAndSettle();

      expect(recorder.paths, <String>['/api/auth/login']);
      expect(recorder.body(0), <String, dynamic>{
        'email': 'admin@fixflow.local',
        'password': 'Admin123!',
      });
      expect(storage.token, 'jwt-for-Administrator');
      expect(auth.user?.role, 'Administrator');
      expect(find.byType(LoginScreen), findsNothing);
      expect(find.byType(HomeScreen), findsOneWidget);
    });

    testWidgets('L9. a rejected login shows the backend message and stores nothing',
        (tester) async {
      _useLargeSurface(tester);
      final _Recorder recorder = _Recorder();
      final _MemoryStorage storage = _MemoryStorage();
      final AuthProvider auth = _auth(
        client: _stub(
          recorder: recorder,
          status: 401,
          body: <String, dynamic>{
            'success': false,
            'message': 'Invalid email or password.',
            'errors': <String>[],
          },
        ),
        storage: storage,
      );
      await tester.pumpWidget(_appWith(auth, const LoginScreen()));
      await tester.pumpAndSettle();

      await tester.enterText(find.byKey(const Key('login_email_field')), 'admin@fixflow.local');
      await tester.enterText(find.byKey(const Key('login_password_field')), 'WrongPass1');
      await tester.tap(find.byKey(const Key('login_submit_button')));
      await tester.pumpAndSettle();

      expect(auth.isAuthenticated, isFalse);
      expect(storage.token, isNull);
      expect(find.byKey(const Key('login_error_banner')), findsOneWidget);
      expect(find.text('Invalid email or password.'), findsOneWidget);
      expect(find.byType(HomeScreen), findsNothing);
    });
  });

  // ===========================================================================
  // AuthProvider against the real endpoint contract
  // ===========================================================================
  group('AuthProvider', () {
    test('A1. a transport failure is reported as a reachable message', () async {
      final _MemoryStorage storage = _MemoryStorage();
      final AuthProvider auth = _auth(
        client: MockClient((http.Request _) async => throw http.ClientException('Connection refused')),
        storage: storage,
      );

      final ok = await auth.login('admin@fixflow.local', 'Admin123!');

      expect(ok, isFalse);
      expect(auth.error, 'Cannot reach the FixFlow API. Check your connection and try again.');
      expect(storage.token, isNull);
    });

    test('A2. logout clears the persisted token and the session', () async {
      final _MemoryStorage storage = _MemoryStorage();
      final AuthProvider auth = _auth(
        client: _stub(recorder: _Recorder(), status: 200, body: _loginResponse(role: 'Manager')),
        storage: storage,
      );

      await auth.login('manager@fixflow.local', 'Manager123!');
      expect(auth.isAuthenticated, isTrue);
      expect(storage.token, isNotNull);

      await auth.logout();

      expect(storage.token, isNull);
      expect(auth.isAuthenticated, isFalse);
      expect(auth.user, isNull);
    });

    test('A3. role landing routes are unchanged', () {
      expect(AppRouter.homeRouteFor('Administrator'), AppRouter.home);
      expect(AppRouter.homeRouteFor('Manager'), AppRouter.home);
      expect(AppRouter.homeRouteFor('Technician'), AppRouter.technicianHome);
      expect(AppRouter.homeRouteFor('Requester'), AppRouter.myRequests);
      expect(AppRouter.homeRouteFor(null), AppRouter.home);
    });
  });

  group('ApiClient error mapping', () {
    Future<String> messageOf(int status, Map<String, dynamic> body) async {
      final ApiClient client = ApiClient(
        client: _stub(recorder: _Recorder(), status: status, body: body),
        storage: _MemoryStorage(),
      );
      try {
        await client.post('/auth/login', <String, dynamic>{});
        fail('expected an AppException for status $status');
      } on AppException catch (e) {
        return e.message;
      }
    }

    test('E1. surfaces the ApiResponse envelope message', () async {
      expect(
        await messageOf(401, <String, dynamic>{
          'success': false,
          'message': 'Invalid email or password.',
          'errors': <String>[],
        }),
        'Invalid email or password.',
      );
    });

    test('E2. surfaces FluentValidation ProblemDetails field errors', () async {
      final message = await messageOf(400, <String, dynamic>{
        'type': 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
        'title': 'One or more validation errors occurred.',
        'status': 400,
        'errors': <String, dynamic>{
          'Email': <String>["'Email' is not a valid email address."],
          'Password': <String>[
            "'Password' must be at least 6 characters. You entered 3 characters."
          ],
        },
      });

      expect(message, contains('is not a valid email address'));
      expect(message, contains('must be at least 6 characters'));
    });

    test('E3. falls back to the ProblemDetails title, then to a generic message',
        () async {
      expect(
        await messageOf(500, <String, dynamic>{
          'title': 'An internal server error occurred.',
          'status': 500,
        }),
        'An internal server error occurred.',
      );
      expect(
        await messageOf(500, <String, dynamic>{'status': 500}),
        'An error occurred',
      );
    });
  });

  // ===========================================================================
  // Registration screen
  // ===========================================================================
  group('RegisterScreen', () {
    Future<void> pumpRegister(WidgetTester tester, AuthProvider auth) async {
      _useLargeSurface(tester);
      await tester.pumpWidget(_appWith(auth, const RegisterScreen()));
      await tester.pumpAndSettle();
    }

    AuthProvider idleAuth() => _auth(
          client: _stub(recorder: _Recorder(), status: 200, body: _envelope(null)),
          storage: _MemoryStorage(),
        );

    Future<void> fillValidForm(WidgetTester tester, {String confirm = 'Passw0rd1'}) async {
      await tester.enterText(find.byKey(const Key('register_first_name_field')), 'Ada');
      await tester.enterText(find.byKey(const Key('register_last_name_field')), 'Lovelace');
      await tester.enterText(find.byKey(const Key('register_email_field')), 'ada@example.com');
      await tester.enterText(find.byKey(const Key('register_phone_field')), '+94771234567');
      await tester.enterText(find.byKey(const Key('register_password_field')), 'Passw0rd1');
      await tester.enterText(find.byKey(const Key('register_confirm_password_field')), confirm);
    }

    testWidgets('R1. renders exactly the fields the backend DTO requires',
        (tester) async {
      await pumpRegister(tester, idleAuth());

      expect(find.text('Join FixFlow'), findsOneWidget);
      expect(find.byKey(const Key('register_role_field')), findsOneWidget);
      expect(find.byKey(const Key('register_first_name_field')), findsOneWidget);
      expect(find.byKey(const Key('register_last_name_field')), findsOneWidget);
      expect(find.byKey(const Key('register_email_field')), findsOneWidget);
      expect(find.byKey(const Key('register_phone_field')), findsOneWidget);
      expect(find.byKey(const Key('register_password_field')), findsOneWidget);
      expect(find.byKey(const Key('register_confirm_password_field')), findsOneWidget);
      expect(find.text('Create Account'), findsOneWidget);
      expect(find.text('Already have an account? Sign in'), findsOneWidget);

      // First, last, email, phone, password, confirm — and nothing invented.
      expect(find.byType(TextFormField), findsNWidgets(6));
    });

    testWidgets('R2. offers only self-service roles, never a privileged one',
        (tester) async {
      await pumpRegister(tester, idleAuth());

      await tester.tap(find.byKey(const Key('register_role_field')));
      await tester.pumpAndSettle();

      // The role list is what the client is allowed to ask the backend for.
      final optionValues = tester
          .widgetList<DropdownMenuItem<String>>(
              find.byType(DropdownMenuItem<String>))
          .map((DropdownMenuItem<String> item) => item.value)
          .toList();
      expect(optionValues, <String>['Requester', 'Technician']);

      expect(find.text('Administrator'), findsNothing);
      expect(find.text('Manager'), findsNothing);
    });

    testWidgets('R3. an empty submit is rejected locally and never calls the API',
        (tester) async {
      final _Recorder recorder = _Recorder();
      final AuthProvider auth = _auth(
        client: _stub(recorder: recorder, status: 200, body: _envelope(null)),
        storage: _MemoryStorage(),
      );
      await pumpRegister(tester, auth);

      await tester.tap(find.byKey(const Key('register_submit_button')));
      await tester.pumpAndSettle();

      expect(find.text('First name is required'), findsOneWidget);
      expect(find.text('Last name is required'), findsOneWidget);
      expect(find.text('Email is required'), findsOneWidget);
      expect(find.text('Phone number is required'), findsOneWidget);
      expect(find.text('Password is required'), findsOneWidget);
      expect(recorder.requests, isEmpty);
    });

    testWidgets('R4. a mismatched confirmation is rejected before submitting',
        (tester) async {
      final _Recorder recorder = _Recorder();
      final AuthProvider auth = _auth(
        client: _stub(recorder: recorder, status: 200, body: _envelope(null)),
        storage: _MemoryStorage(),
      );
      await pumpRegister(tester, auth);

      await fillValidForm(tester, confirm: 'Passw0rd2');
      await tester.tap(find.byKey(const Key('register_submit_button')));
      await tester.pumpAndSettle();

      expect(find.text('Passwords do not match'), findsOneWidget);
      expect(recorder.requests, isEmpty,
          reason: 'a mismatched confirmation must never reach the backend');
    });

    testWidgets('R5. a too-short password is rejected', (tester) async {
      final _Recorder recorder = _Recorder();
      final AuthProvider auth = _auth(
        client: _stub(recorder: recorder, status: 200, body: _envelope(null)),
        storage: _MemoryStorage(),
      );
      await pumpRegister(tester, auth);

      await fillValidForm(tester, confirm: 'Ab1');
      await tester.enterText(find.byKey(const Key('register_password_field')), 'Ab1');
      await tester.tap(find.byKey(const Key('register_submit_button')));
      await tester.pumpAndSettle();

      expect(find.text('Use at least 8 characters'), findsOneWidget);
      expect(recorder.requests, isEmpty);
    });

    testWidgets('R6. a password without a letter and a number is rejected',
        (tester) async {
      final _Recorder recorder = _Recorder();
      final AuthProvider auth = _auth(
        client: _stub(recorder: recorder, status: 200, body: _envelope(null)),
        storage: _MemoryStorage(),
      );
      await pumpRegister(tester, auth);

      await fillValidForm(tester, confirm: 'abcdefgh');
      await tester.enterText(find.byKey(const Key('register_password_field')), 'abcdefgh');
      await tester.tap(find.byKey(const Key('register_submit_button')));
      await tester.pumpAndSettle();

      expect(find.text('Include at least one letter and one number'), findsOneWidget);
      expect(recorder.requests, isEmpty);
    });

    testWidgets('R7. a malformed email and phone are rejected', (tester) async {
      final _Recorder recorder = _Recorder();
      final AuthProvider auth = _auth(
        client: _stub(recorder: recorder, status: 200, body: _envelope(null)),
        storage: _MemoryStorage(),
      );
      await pumpRegister(tester, auth);

      await fillValidForm(tester);
      await tester.enterText(find.byKey(const Key('register_email_field')), 'ada-at-example');
      await tester.enterText(find.byKey(const Key('register_phone_field')), '12345');
      await tester.tap(find.byKey(const Key('register_submit_button')));
      await tester.pumpAndSettle();

      expect(find.text('Enter a valid email address'), findsOneWidget);
      expect(find.text('Enter a valid phone number'), findsOneWidget);
      expect(recorder.requests, isEmpty);
    });

    testWidgets('R8. the loading state disables submit and blocks a second request',
        (tester) async {
      _useLargeSurface(tester);
      final _Recorder recorder = _Recorder();
      final AuthProvider auth = _auth(
        client: _hanging(recorder: recorder),
        storage: _MemoryStorage(),
      );
      await tester.pumpWidget(_appWith(auth, const RegisterScreen()));
      await tester.pumpAndSettle();

      await fillValidForm(tester);
      await tester.tap(find.byKey(const Key('register_submit_button')));
      await tester.pump();

      expect(auth.isLoading, isTrue);
      expect(
        tester.widget<ElevatedButton>(find.byKey(const Key('register_submit_button'))).onPressed,
        isNull,
      );
      expect(find.byType(CircularProgressIndicator), findsOneWidget);

      await tester.tap(find.byKey(const Key('register_submit_button')), warnIfMissed: false);
      await tester.pump();

      expect(recorder.requests, hasLength(1),
          reason: 'registration must not be submitted twice');
    });

    testWidgets('R9. posts the real RegisterRequestDto contract, then auto signs in',
        (tester) async {
      final _Recorder recorder = _Recorder();
      final _MemoryStorage storage = _MemoryStorage();
      final AuthProvider auth = _auth(
        client: _registerThenLogin(recorder: recorder),
        storage: storage,
      );

      final ok = await auth.register(
        firstName: 'Ada',
        lastName: 'Lovelace',
        email: 'ada@example.com',
        phoneNumber: '+94771234567',
        password: 'Passw0rd1',
      );

      expect(ok, isTrue);
      expect(recorder.paths, <String>['/api/auth/register', '/api/auth/login']);
      expect(recorder.body(0), <String, dynamic>{
        'firstName': 'Ada',
        'lastName': 'Lovelace',
        'email': 'ada@example.com',
        'phoneNumber': '+94771234567',
        'password': 'Passw0rd1',
        'roleName': 'Requester',
      });
      expect(storage.token, 'jwt-for-Requester');
      expect(auth.user?.role, 'Requester');
    });

    testWidgets('R10. a duplicate email shows the backend conflict message',
        (tester) async {
      _useLargeSurface(tester);
      final _Recorder recorder = _Recorder();
      final _MemoryStorage storage = _MemoryStorage();
      final AuthProvider auth = _auth(
        client: _stub(
          recorder: recorder,
          status: 409,
          body: <String, dynamic>{
            'success': false,
            'message': 'User with this email already exists.',
            'errors': <String>[],
          },
        ),
        storage: storage,
      );
      await tester.pumpWidget(_appWith(auth, const RegisterScreen()));
      await tester.pumpAndSettle();

      await fillValidForm(tester);
      await tester.tap(find.byKey(const Key('register_submit_button')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('register_error_banner')), findsOneWidget);
      expect(find.text('User with this email already exists.'), findsOneWidget);
      expect(recorder.paths, <String>['/api/auth/register']);
      expect(storage.token, isNull);
      expect(auth.isAuthenticated, isFalse);
    });

    testWidgets('R11. server-side validation errors are displayed, not swallowed',
        (tester) async {
      _useLargeSurface(tester);
      final AuthProvider auth = _auth(
        client: _stub(
          recorder: _Recorder(),
          status: 400,
          body: <String, dynamic>{
            'type': 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
            'title': 'One or more validation errors occurred.',
            'status': 400,
            'errors': <String, dynamic>{
              'Email': <String>["'Email' is not a valid email address."],
            },
          },
        ),
        storage: _MemoryStorage(),
      );
      await tester.pumpWidget(_appWith(auth, const RegisterScreen()));
      await tester.pumpAndSettle();

      await fillValidForm(tester);
      await tester.tap(find.byKey(const Key('register_submit_button')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('register_error_banner')), findsOneWidget);
      expect(
        find.textContaining('is not a valid email address'),
        findsOneWidget,
      );
    });

    testWidgets('R12. the sign-in link returns to Login', (tester) async {
      _useLargeSurface(tester);
      final AuthProvider auth = _auth(
        client: _stub(recorder: _Recorder(), status: 200, body: _envelope(null)),
        storage: _MemoryStorage(),
      );
      await tester.pumpWidget(_routedApp(auth));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('login_register_link')));
      await tester.pumpAndSettle();
      expect(find.byType(RegisterScreen), findsOneWidget);

      await tester.tap(find.byKey(const Key('register_login_link')));
      await tester.pumpAndSettle();

      expect(find.byType(LoginScreen), findsOneWidget);
      expect(find.byType(RegisterScreen), findsNothing);
    });
  });

  // ===========================================================================
  // Component 2 home screen entry points
  // ===========================================================================
  group('HomeScreen Component 2 entry points', () {
    testWidgets('H1. Administrator keeps both remaining C2 actions',
        (tester) async {
      final AuthProvider auth = await _signedIn('Administrator');
      await tester.pumpWidget(_appWith(auth, const HomeScreen()));
      await tester.pumpAndSettle();

      expect(find.text('Request Queue'), findsOneWidget);
      expect(find.text('Priority & SLA Tracking'), findsOneWidget);
      expect(find.text('Risk & Priority Overview'), findsNothing,
          reason: 'the duplicate home entry point was removed');
    });

    testWidgets('H2. Manager keeps both remaining C2 actions', (tester) async {
      final AuthProvider auth = await _signedIn('Manager');
      await tester.pumpWidget(_appWith(auth, const HomeScreen()));
      await tester.pumpAndSettle();

      expect(find.text('Request Queue'), findsOneWidget);
      expect(find.text('Priority & SLA Tracking'), findsOneWidget);
      expect(find.text('Risk & Priority Overview'), findsNothing);
    });

    testWidgets('H3. Technician keeps read-only Component 2 access',
        (tester) async {
      final AuthProvider auth = await _signedIn('Technician');
      await tester.pumpWidget(_appWith(auth, const HomeScreen()));
      await tester.pumpAndSettle();

      expect(find.text('Priority & Risk Information (Read-only)'), findsOneWidget);
      expect(find.text('My Assigned Jobs'), findsOneWidget,
          reason: 'the Component 3 entry point must be untouched');
      expect(find.text('Risk & Priority Overview'), findsNothing);
      expect(find.text('Request Queue'), findsNothing);
    });

    testWidgets('H4. Requester home is unchanged', (tester) async {
      final AuthProvider auth = await _signedIn('Requester');
      await tester.pumpWidget(_appWith(auth, const HomeScreen()));
      await tester.pumpAndSettle();

      expect(find.text('Submit a Maintenance Request'), findsOneWidget);
      expect(find.text('My Requests'), findsOneWidget);
      expect(find.text('Risk & Priority Tracking'), findsOneWidget);
      expect(find.text('Risk & Priority Overview'), findsNothing);
      expect(find.text('Request Queue'), findsNothing);
    });

    testWidgets('H5. "Priority & SLA Tracking" still opens the existing C2 screen',
        (tester) async {
      final AuthProvider auth = await _signedIn('Administrator');
      await tester.pumpWidget(_routedApp(auth, initial: AppRouter.home));
      await tester.pumpAndSettle();

      expect(find.byType(HomeScreen), findsOneWidget);

      await tester.tap(find.text('Priority & SLA Tracking'));
      // Deliberate pumps rather than pumpAndSettle: the C2 screen shows an
      // indeterminate spinner while it loads, which never settles.
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 400));

      expect(find.byType(PriorityDetailsScreen), findsOneWidget);
    });
  });
}
