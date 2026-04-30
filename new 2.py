import pandas as pd
import scipy.io as scipy
import os as os


def ler_arquivos_dat(caminho_arquivo):
    mat = scipy.loadmat(caminho_arquivo)
    data = mat['data']
    df = pd.DataFrame(data, columns=['tamanho', 'numero', 'preco'])
    return df

def ler_arquivos_csv(caminho_arquivo):
    data = pd.read_csv(caminho_arquivo, header=None)
    df = pd.DataFrame(data, columns=['tamanho', 'numero', 'preco'])
    return df


def main():
   arquivo_usuario = input("Digite o nome do arquivo que você subiu (ex: dados.mat ou teste.csv): ")

   nome_arquivo = arquivo_usuario
   nome, extensao = os.path.splitext(nome_arquivo)

   arquivo = "";

   if extensao.lower() == ".mat":
     df = ler_arquivos_dat(nome_arquivo)
     print("li o arquivo mat")
   elif extensao.lower() == ".csv":
    df = ler_arquivos_csv(nome_arquivo)
    print("li o arquivo csv")
   else:
     print(f"Arquivo não suportado")
     return

   print("Análise Estastística")
   print(df.describe());
   
   
   media = df['preco'].mean()
   print(f"Média: {media}")
   
   menor_casa = df.sort_values(by='tamanho').iloc[0]
   print(f"Menor Casa: {menor_casa} e quanto custa: {menor_casa['preco']}")
   
   casa_mais_cara =  df.loc[df['preco'].idxmax()]
   print(f"Casa mais cara custa : {casa_mais_cara['preco']}")
   
   #h



if __name__ == "__main__":
    main()