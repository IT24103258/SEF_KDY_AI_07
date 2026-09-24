import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../providers/theme_provider.dart';
import '../core/routes/app_router.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context);
    final themeProvider = Provider.of<ThemeProvider>(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('FixFlow Mobile Dashboard'),
        actions: [
          IconButton(
            icon: Icon(themeProvider.isDarkMode ? Icons.light_mode : Icons.dark_mode),
            onPressed: () => themeProvider.toggleTheme(),
          ),
          IconButton(
            icon: const Icon(Icons.person),
            onPressed: () => Navigator.pushNamed(context, AppRouter.profile),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Welcome card (unchanged) ──────────────────────────────────
            Card(
              child: ListTile(
                leading: const CircleAvatar(
                  backgroundColor: Color(0xFF2563EB),
                  child: Icon(Icons.person, color: Colors.white),
                ),
                title: Text(
                  'Welcome, ${authProvider.user?.firstName ?? 'User'}!',
                  style: const TextStyle(fontWeight: FontWeight.bold),
                ),
                subtitle: Text('Role: ${authProvider.user?.role ?? 'Requester'}'),
              ),
            ),
            const SizedBox(height: 24),

            // ── Gateway info card (unchanged) ─────────────────────────────
            const Text(
              'Mobile Workflows Gateway',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 12),
            const Card(
              child: Padding(
                padding: EdgeInsets.all(16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'API Gateway Connection: Active',
                      style: TextStyle(fontWeight: FontWeight.bold, color: Colors.green),
                    ),
                    SizedBox(height: 8),
                    Text(
                      'This Flutter mobile app connects exclusively to ASP.NET Core Web API. Direct database or Python agent access is strictly prohibited.',
                      style: TextStyle(fontSize: 12, color: Colors.grey),
                    ),
                  ],
                ),
              ),
            ),

            // ──────────────────────────────────────────────────────────────
            // COMPONENT 4 — TECHNICIAN WORKSPACE NAVIGATION
            // Only visible when the authenticated user's role is 'Technician'.
            // All routes are already registered in AppRouter (no new routes).
            // All target screens already exist (no new screens created).
            // Manager and Requester are completely unaffected.
            // ──────────────────────────────────────────────────────────────
            if (authProvider.user?.role == 'Technician') ...[
              const SizedBox(height: 24),
              const Text(
                'My Technician Workspace',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 12),
              Card(
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                child: Column(
                  children: [
                    _TechNavTile(
                      icon: Icons.calendar_today,
                      label: 'My Schedule / Calendar',
                      subtitle: 'View daily scheduled jobs',
                      color: const Color(0xFF2563EB),
                      onTap: () => Navigator.pushNamed(
                        context,
                        AppRouter.technicianSchedule,
                      ),
                    ),
                    const Divider(height: 1, indent: 56),
                    _TechNavTile(
                      icon: Icons.assignment,
                      label: 'All Assigned Work Orders',
                      subtitle: 'Browse and filter all jobs',
                      color: Colors.orange,
                      onTap: () => Navigator.pushNamed(
                        context,
                        AppRouter.allJobs,
                      ),
                    ),
                    const Divider(height: 1, indent: 56),
                    _TechNavTile(
                      icon: Icons.notifications_outlined,
                      label: 'Notifications',
                      subtitle: 'View recent alerts',
                      color: Colors.teal,
                      onTap: () => Navigator.pushNamed(
                        context,
                        AppRouter.notifications,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 8),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 4),
                child: Text(
                  'Tap a job in Schedule or Work Orders to open job details, '
                  'start work, add field notes, and capture completion sign-off.',
                  style: TextStyle(fontSize: 11, color: Colors.grey[600]),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

// ────────────────────────────────────────────────────────────────────────────
// Private helper widget — only used inside HomeScreen.
// A single navigation tile for the Technician workspace card.
// ────────────────────────────────────────────────────────────────────────────
class _TechNavTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final String subtitle;
  final Color color;
  final VoidCallback onTap;

  const _TechNavTile({
    required this.icon,
    required this.label,
    required this.subtitle,
    required this.color,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: CircleAvatar(
        backgroundColor: color.withValues(alpha: 0.12),
        child: Icon(icon, color: color, size: 22),
      ),
      title: Text(
        label,
        style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
      ),
      subtitle: Text(
        subtitle,
        style: TextStyle(fontSize: 12, color: Colors.grey[600]),
      ),
      trailing: const Icon(Icons.chevron_right, size: 20),
      onTap: onTap,
    );
  }
}
