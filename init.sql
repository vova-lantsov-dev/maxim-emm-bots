CREATE TABLE IF NOT EXISTS public.users (
    user_id integer primary key not null generated always as identity,
    username text not null,
    password_hash text not null,
    password_salt text not null,
    user_role text not null);

INSERT INTO public.users(username, password_hash, password_salt, user_role) VALUES
    ('admin', 'pnEbmwX+32jjhUisKfnLHTd7fEAskmr3Z/hsFw7RYQIpSdzv', 'pnEbmwX+32jjhUisKfnLHQ==', 'admin'),
    ('readonly', 'mrUjhpskcfPpHXhHLbztqK5RQswEZQEnfjroFKRJo9dVxxLb', 'mrUjhpskcfPpHXhHLbztqA==', 'readonly')
    ON CONFLICT DO NOTHING;