import { NavLink, Route, Routes, useLocation } from 'react-router';
import { Separator } from '@/components/ui/separator';
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarRail,
  SidebarTrigger,
} from '@/components/ui/sidebar';
import { Toaster } from '@/components/ui/sonner';
import type { ModuleRoute } from '@/modules';
import { Home } from '@/pages/Home';
import { Breadcrumbs } from '@/shell/Breadcrumbs';
import { CommandPalette } from '@/shell/CommandPalette';
import { ThemeToggle } from '@/theme/ThemeToggle';
import { UserMenu } from '@/auth/UserMenu';

const moduleRoutes = Object.values(
  import.meta.glob<ModuleRoute>('./modules/*/routes.tsx', { eager: true, import: 'route' }),
);

/** Whether the address is the page's or below it. */
function isAt(pathname: string, path: string) {
  return pathname === path || pathname.startsWith(`${path}/`);
}

/**
 * The app shell: a sidebar with every module's page (each module's routes.tsx adds its own), the breadcrumbs and the
 * command palette above the page, and the toasts.
 */
export function App() {
  const { pathname } = useLocation();
  return (
    <SidebarProvider>
      <Sidebar>
        <SidebarHeader>
          <p className="px-2 py-1 font-semibold">fabric-pilot</p>
        </SidebarHeader>
        <SidebarContent>
          <SidebarGroup>
            <SidebarGroupLabel>Modules</SidebarGroupLabel>
            <SidebarGroupContent>
              <nav aria-label="Modules">
                <SidebarMenu>
                  <SidebarMenuItem>
                    <SidebarMenuButton asChild isActive={pathname === '/'}>
                      <NavLink to="/" end viewTransition>
                        Home
                      </NavLink>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                  {moduleRoutes.map((route) => (
                    <SidebarMenuItem key={route.path}>
                      <SidebarMenuButton asChild isActive={isAt(pathname, `/${route.path}`)}>
                        <NavLink to={route.path} viewTransition>
                          {route.title}
                        </NavLink>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  ))}
                </SidebarMenu>
              </nav>
            </SidebarGroupContent>
          </SidebarGroup>
        </SidebarContent>
        <SidebarFooter>
          <ThemeToggle />
          <UserMenu />
        </SidebarFooter>
        <SidebarRail />
      </Sidebar>
      <SidebarInset>
        <header className="flex h-12 shrink-0 items-center gap-2 border-b bg-glass px-4 backdrop-blur-md">
          <SidebarTrigger className="-ml-1" />
          <Separator orientation="vertical" className="mr-2 data-[orientation=vertical]:h-4" />
          <Breadcrumbs routes={moduleRoutes} />
          <CommandPalette routes={moduleRoutes} />
        </header>
        <Routes>
          <Route path="/" element={<Home />} />
          {moduleRoutes.map((route) => (
            <Route key={route.path} path={`${route.path}/*`} element={route.element} />
          ))}
        </Routes>
      </SidebarInset>
      <Toaster />
    </SidebarProvider>
  );
}
