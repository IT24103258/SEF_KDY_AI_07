import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../core/routes/app_router.dart';
import '../widgets/auth_page_scaffold.dart';

/// Registration screen — /register.
///
/// Mirrors the React Register page: same POST /api/auth/register contract,
/// same client-side validation rules, and the same auto-login-after-register
/// flow. Only Requester and Technician accounts can be self-registered
/// (backend seeds Manager/Administrator separately).
class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();

  final _firstNameCtrl = TextEditingController();
  final _lastNameCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  final _confirmCtrl = TextEditingController();

  String _roleName = 'Requester';
  bool _obscurePassword = true;
  bool _obscureConfirm = true;

  static final RegExp _emailRe = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$');
  static final RegExp _phoneRe = RegExp(r'^\+?[0-9]{9,15}$');

  @override
  void dispose() {
    _firstNameCtrl.dispose();
    _lastNameCtrl.dispose();
    _emailCtrl.dispose();
    _phoneCtrl.dispose();
    _passwordCtrl.dispose();
    _confirmCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();

    final authProvider = context.read<AuthProvider>();
    final success = await authProvider.register(
      firstName: _firstNameCtrl.text.trim(),
      lastName: _lastNameCtrl.text.trim(),
      email: _emailCtrl.text.trim(),
      phoneNumber: _phoneCtrl.text.trim(),
      password: _passwordCtrl.text,
      roleName: _roleName,
    );

    if (!mounted) return;
    if (success) {
      Navigator.pushReplacementNamed(
          context, AppRouter.homeRouteFor(authProvider.user?.role));
    }
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = context.watch<AuthProvider>();
    final isLoading = authProvider.isLoading;

    return AuthPageScaffold(
      title: 'Join FixFlow',
      subtitle: 'Register as a customer to raise requests, or as a '
          'technician to get assigned work.',
      showBackButton: true,
      children: [
        if (authProvider.error != null) ...[
          AuthErrorBanner(
            key: const Key('register_error_banner'),
            message: authProvider.error!,
          ),
          const SizedBox(height: 18),
        ],
        Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              DropdownButtonFormField<String>(
                key: const Key('register_role_field'),
                initialValue: _roleName,
                isExpanded: true,
                decoration: const InputDecoration(
                  labelText: "I'm registering as",
                  border: OutlineInputBorder(),
                  prefixIcon: Icon(Icons.badge_outlined),
                ),
                items: const [
                  DropdownMenuItem(
                    value: 'Requester',
                    child: Text('Customer — I want to submit requests'),
                  ),
                  DropdownMenuItem(
                    value: 'Technician',
                    child: Text('Technician — I carry out work orders'),
                  ),
                ],
                // The closed field is only one line tall, so it shows the short
                // role name; the menu keeps the descriptive labels.
                selectedItemBuilder: (BuildContext context) => const [
                  Text('Customer (Requester)'),
                  Text('Technician'),
                ],
                onChanged: isLoading
                    ? null
                    : (v) => setState(() => _roleName = v ?? 'Requester'),
              ),
              const SizedBox(height: 16),

              Row(children: [
                Expanded(
                  child: TextFormField(
                    key: const Key('register_first_name_field'),
                    controller: _firstNameCtrl,
                    enabled: !isLoading,
                    textCapitalization: TextCapitalization.words,
                    textInputAction: TextInputAction.next,
                    autofillHints: const [AutofillHints.givenName],
                    decoration: const InputDecoration(
                      labelText: 'First Name *',
                      border: OutlineInputBorder(),
                      prefixIcon: Icon(Icons.person_outline),
                    ),
                    validator: (v) => (v == null || v.trim().isEmpty)
                        ? 'First name is required'
                        : null,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    key: const Key('register_last_name_field'),
                    controller: _lastNameCtrl,
                    enabled: !isLoading,
                    textCapitalization: TextCapitalization.words,
                    textInputAction: TextInputAction.next,
                    autofillHints: const [AutofillHints.familyName],
                    decoration: const InputDecoration(
                      labelText: 'Last Name *',
                      border: OutlineInputBorder(),
                      prefixIcon: Icon(Icons.person_outline),
                    ),
                    validator: (v) => (v == null || v.trim().isEmpty)
                        ? 'Last name is required'
                        : null,
                  ),
                ),
              ]),
              const SizedBox(height: 16),

              TextFormField(
                key: const Key('register_email_field'),
                controller: _emailCtrl,
                enabled: !isLoading,
                keyboardType: TextInputType.emailAddress,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.email],
                decoration: const InputDecoration(
                  labelText: 'Email Address *',
                  hintText: 'you@example.com',
                  border: OutlineInputBorder(),
                  prefixIcon: Icon(Icons.alternate_email),
                ),
                validator: (v) {
                  if (v == null || v.trim().isEmpty) {
                    return 'Email is required';
                  }
                  if (!_emailRe.hasMatch(v.trim())) {
                    return 'Enter a valid email address';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),

              TextFormField(
                key: const Key('register_phone_field'),
                controller: _phoneCtrl,
                enabled: !isLoading,
                keyboardType: TextInputType.phone,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.telephoneNumber],
                decoration: const InputDecoration(
                  labelText: 'Phone Number *',
                  hintText: '+94 77 123 4567',
                  border: OutlineInputBorder(),
                  prefixIcon: Icon(Icons.phone_outlined),
                ),
                validator: (v) {
                  final cleaned =
                      (v ?? '').replaceAll(RegExp(r'[\s\-()]'), '');
                  if (cleaned.isEmpty) return 'Phone number is required';
                  if (!_phoneRe.hasMatch(cleaned)) {
                    return 'Enter a valid phone number';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),

              TextFormField(
                key: const Key('register_password_field'),
                controller: _passwordCtrl,
                enabled: !isLoading,
                obscureText: _obscurePassword,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.newPassword],
                decoration: InputDecoration(
                  labelText: 'Password *',
                  hintText: 'At least 8 characters',
                  border: const OutlineInputBorder(),
                  prefixIcon: const Icon(Icons.lock_outline),
                  suffixIcon: IconButton(
                    key: const Key('register_password_toggle'),
                    icon: Icon(_obscurePassword
                        ? Icons.visibility_off_outlined
                        : Icons.visibility_outlined),
                    tooltip: _obscurePassword
                        ? 'Show password'
                        : 'Hide password',
                    onPressed: () =>
                        setState(() => _obscurePassword = !_obscurePassword),
                  ),
                ),
                validator: (v) {
                  if (v == null || v.isEmpty) return 'Password is required';
                  if (v.length < 8) return 'Use at least 8 characters';
                  if (!RegExp(r'[A-Za-z]').hasMatch(v) ||
                      !RegExp(r'[0-9]').hasMatch(v)) {
                    return 'Include at least one letter and one number';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),

              TextFormField(
                key: const Key('register_confirm_password_field'),
                controller: _confirmCtrl,
                enabled: !isLoading,
                obscureText: _obscureConfirm,
                textInputAction: TextInputAction.done,
                autofillHints: const [AutofillHints.newPassword],
                onFieldSubmitted: isLoading ? null : (_) => _submit(),
                decoration: InputDecoration(
                  labelText: 'Confirm Password *',
                  border: const OutlineInputBorder(),
                  prefixIcon: const Icon(Icons.lock_reset_outlined),
                  suffixIcon: IconButton(
                    key: const Key('register_confirm_password_toggle'),
                    icon: Icon(_obscureConfirm
                        ? Icons.visibility_off_outlined
                        : Icons.visibility_outlined),
                    tooltip: _obscureConfirm
                        ? 'Show password'
                        : 'Hide password',
                    onPressed: () =>
                        setState(() => _obscureConfirm = !_obscureConfirm),
                  ),
                ),
                validator: (v) =>
                    v != _passwordCtrl.text ? 'Passwords do not match' : null,
              ),
            ],
          ),
        ),
        const SizedBox(height: 24),

        SizedBox(
          height: 50,
          child: ElevatedButton(
            key: const Key('register_submit_button'),
            onPressed: isLoading ? null : _submit,
            style: ElevatedButton.styleFrom(
              backgroundColor: AuthPageScaffold.brandBlue,
              foregroundColor: Colors.white,
              disabledBackgroundColor:
                  AuthPageScaffold.brandBlue.withValues(alpha: 0.55),
              disabledForegroundColor: Colors.white70,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
              ),
            ),
            child: isLoading
                ? const SizedBox(
                    width: 22,
                    height: 22,
                    child: CircularProgressIndicator(
                        strokeWidth: 2, color: Colors.white),
                  )
                : const Text('Create Account',
                    style:
                        TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
          ),
        ),
        const SizedBox(height: 6),

        TextButton(
          key: const Key('register_login_link'),
          onPressed: isLoading
              ? null
              : () => AuthPageScaffold.backToLogin(context),
          child: const Text('Already have an account? Sign in'),
        ),
      ],
    );
  }
}
