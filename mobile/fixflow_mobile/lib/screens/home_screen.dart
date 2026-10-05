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
            Card(
              child: ListTile(
                leading: const CircleAvatar(
                  backgroundColor: Color(0xFF2563EB),
                  child: Icon(Icons.person, color: Colors.white),
                ),
                title: Text(
                  'Welcome, ${authProvider.user?.firstName ?? 'there'}!',
                  style: const TextStyle(fontWeight: FontWeight.bold),
                ),
                subtitle: Text('Role: ${authProvider.user?.role ?? '—'}'),
              ),
            ),
            const SizedBox(height: 20),

            // Technician Quick Access Card — Technician workflow
            if (role == 'Technician')
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
            ] else if (role == 'Technician') ...[
              // Component 3 workflow stays exactly as it was: the assigned-jobs
              // card above is the technician's entry point.
              //
              // Component 2 read-only view. Reuses the existing PriorityDetailsScreen
              // and its existing any-authenticated route. That screen already gates
              // every management action behind its own Manager/Administrator check,
              // so a technician sees risk / priority / SLA / explainability but never
              // Override, Escalate or De-escalate.
              _QuickAction(
                icon: Icons.shield_outlined,
                label: 'Priority & Risk Information (Read-only)',
                onTap: () => Navigator.pushNamed(context, AppRouter.priorityDetails),
              ),
            ] else ...[
              // Manager / Administrator workflow — classification review &
              // override happens from a request in the queue.
              _QuickAction(
                icon: Icons.pending_actions,
                label: 'Request Queue',
                onTap: () => Navigator.pushNamed(context, AppRouter.requestQueue),
              ),
              _QuickAction(
                icon: Icons.shield_outlined,
                label: 'Priority & SLA Tracking',
                onTap: () => Navigator.pushNamed(context, AppRouter.priorityDetails),
              ),
              // The standalone Risk Matrix stays reachable from its tool card
              // inside PriorityDetailsScreen, so it is not duplicated here.
            ],
          ],
        ),
      ),
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