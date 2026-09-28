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
    final role = authProvider.user?.role ?? '';

    return Scaffold(
      appBar: AppBar(
        title: const Text('FixFlow'),
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
                  'Welcome, ${authProvider.user?.firstName ?? 'Technician'}!',
                  style: const TextStyle(fontWeight: FontWeight.bold),
                ),
                subtitle: Text('Role: ${authProvider.user?.role ?? 'Technician'}'),
              ),
            ),
            const SizedBox(height: 20),

            // Technician Quick Access Card
            Card(
              color: const Color(0xFF2563EB).withOpacity(0.08),
              child: ListTile(
                leading: const Icon(Icons.build_circle, color: Color(0xFF2563EB), size: 36),
                title: const Text('My Assigned Jobs', style: TextStyle(fontWeight: FontWeight.bold)),
                subtitle: const Text('View and update assigned maintenance tasks'),
                trailing: const Icon(Icons.arrow_forward_ios, size: 16),
                onTap: () {
                  Navigator.pushNamed(context, AppRouter.technicianHome);
                },
              ),
            ),

            const SizedBox(height: 24),

            // ── Gateway info card (unchanged) ─────────────────────────────
            const Text(
              'Quick Actions',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 12),

            if (role == 'Requester') ...[
              _QuickAction(
                icon: Icons.add_circle_outline,
                label: 'Submit a Maintenance Request',
                onTap: () => Navigator.pushNamed(context, AppRouter.submitRequest),
              ),
              _QuickAction(
                icon: Icons.list_alt,
                label: 'My Requests',
                onTap: () => Navigator.pushNamed(context, AppRouter.myRequests),
              ),
              const SizedBox(height: 16),
              Card(
                child: ListTile(
                  leading: const CircleAvatar(
                    backgroundColor: Color(0xFFF59E0B),
                    child: Icon(Icons.shield_outlined, color: Colors.white),
                  ),
                  title: const Text(
                    'Risk & Priority Tracking',
                    style: TextStyle(fontWeight: FontWeight.bold),
                  ),
                  subtitle: const Text(
                    'View deterministic priority, SLAs & safety hazards',
                  ),
                  trailing: const Icon(Icons.arrow_forward_ios, size: 14),
                  onTap: () => Navigator.pushNamed(
                    context,
                    AppRouter.priorityDetails,
                  ),
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
            ] else ...[
              // Staff roles (Manager/Administrator/Technician) land here for now.
              // Other members: add your own quick actions in this block.
              _QuickAction(
                icon: Icons.list_alt,
                label: 'My Requests',
                onTap: () => Navigator.pushNamed(context, AppRouter.myRequests),
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
        backgroundColor: color.withOpacity(0.12),
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
class _QuickAction extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  const _QuickAction({required this.icon, required this.label, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        leading: Icon(icon, color: const Color(0xFF2563EB)),
        title: Text(label),
        trailing: const Icon(Icons.chevron_right),
        onTap: onTap,
      ),
    );
  }
}
